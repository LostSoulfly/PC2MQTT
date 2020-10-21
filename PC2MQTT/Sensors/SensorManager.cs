using BadLogger;
using ExtensionMethods;
using PC2MQTT.Helpers;
using PC2MQTT.MQTT;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace PC2MQTT.Sensors
{
    public class SensorManager : IDisposable
    {
        public ConcurrentDictionary<string, SensorHost> sensors = new ConcurrentDictionary<string, SensorHost>();
        private IClient _client;

        private Helpers.Settings _settings;
        private BadLogger.BadLogger Log;
        private System.Timers.Timer sensorCleanupTimer;
        private ConcurrentDictionary<string, SensorHost> sensorMultiLevelWildcardTopics;
        private ConcurrentDictionary<string, SensorHost> sensorSingleLevelWildcardTopics;
        private ConcurrentDictionary<string, SensorHost> sensorTopics;

        public SensorManager(IClient client, Helpers.Settings settings)
        {
            this._client = client;
            this._settings = settings;

            Log = LogManager.GetCurrentClassLogger("SensorManager");
            sensorCleanupTimer = new System.Timers.Timer(60000);
            sensorCleanupTimer.Elapsed += SensorCleanupTimer_Elapsed;

            sensorTopics = new ConcurrentDictionary<string, SensorHost>();
            sensorMultiLevelWildcardTopics = new ConcurrentDictionary<string, SensorHost>();
            sensorSingleLevelWildcardTopics = new ConcurrentDictionary<string, SensorHost>();
        }

        public void ReMapTopics()
        {

            foreach (var item in sensorTopics)
            {
                _client.Subscribe(new MqttMessageBuilder().SubscribeMessage.QueueMessage.AddTopic(item.Key).Build());
            }

            foreach (var item in sensorMultiLevelWildcardTopics)
            {
                _client.Subscribe(new MqttMessageBuilder().SubscribeMessage.AddTopic(item.Key).Build());
            }

            foreach (var item in sensorSingleLevelWildcardTopics)
            {
                _client.Subscribe(new MqttMessageBuilder().SubscribeMessage.AddTopic(item.Key).Build());
            }

        }

        public void Dispose()
        {
            Log.Info("Disposing of SensorManager..");

            foreach (var item in sensors)
            {
                if (item.Value.IsCompiled)
                {
                    Log.Debug($"Disposing sensor [{item.Value.SensorIdentifier}]");
                    item.Value.Dispose();
                    sensors.TryRemove(item.Value.SensorIdentifier, out var s);
                    DisposeSensorHost(item.Value);
                }
            }
        }

        public void DisposeSensorHost(SensorHost sensorHost)
        {
            sensorHost.Dispose();
            sensorHost = null;
        }

        public void InitializeSensors(List<string> enabledSensors)
        {
            Log.Trace("Initializing sensors..");

            foreach (var item in sensors)
            {
                Task.Run(() =>
                {
                    if ((enabledSensors.Contains("*") || enabledSensors.Contains(item.Value.SensorIdentifier)) && item.Value.InitializeSensor())
                    {
                        Log.Info($"Initialized sensor: [{item.Value.SensorIdentifier}]");
                    }
                    else
                    {
                        Log.Debug($"Skipping sensor: [{item.Value.SensorIdentifier}]");
                    }
                });
            }
        }

        public List<string> LoadSensorScripts()
        {
            /* Load built-in sensors
            var type = typeof(ISensor);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p)).ToList();
            */

            List<string> availableSensors = new List<string>();

            var tasks = new List<Task<SensorHost>>();

            var sensorFiles = Directory.GetFiles("sensors/", "*.cs").ToList();

            Log.Info("Compiling sensor scripts. You can use only built-in sensors by setting useOnlyBuiltInScripts in config.json.");
            foreach (var item in sensorFiles)
            {
                //Task<SensorHost> t;
                //t = Task.Run(() => LoadSensor(item));
                tasks.Add(Task.Run(() => LoadSensor(item)));

                //availableSensors.Add(t.Result.SensorIdentifier);
            }

            Log.Debug("Waiting for all sensors to finish loading..");
            Task.WaitAll(tasks.ToArray());

            availableSensors.AddRange(tasks.Select(item => item.Result.SensorIdentifier).Where(ident => ident != null));

            return availableSensors;
        }

        public bool MapTopicToSensor(MqttMessage mqttMessage, SensorHost sensorHost, bool prependDeviceId = true)
        {
            Log.Debug($"Mapping topic [{mqttMessage.GetRawTopic()}] for [{sensorHost.SensorIdentifier}]");

            bool result;
            try
            {
                if (!mqttMessage.GetRawTopic().ValidateTopic())
                    return false;
            } catch (Exception ex)
            {
                Log.Debug($"Topic {mqttMessage.GetRawTopic()} validated false: {ex.Message}");
                return false;
            }

            if (mqttMessage.GetRawTopic().Contains("#")) // multi-level wildcard topic
            {
                result = sensorMultiLevelWildcardTopics.TryAdd(mqttMessage.GetRawTopic(), sensorHost);

            }
            else if (mqttMessage.GetRawTopic().Contains("+"))// single level wildcard topic
            {
                result = sensorSingleLevelWildcardTopics.TryAdd(mqttMessage.GetRawTopic(), sensorHost);
            }
            else
            {
                result = sensorTopics.TryAdd(mqttMessage.GetRawTopic(), sensorHost);
            }

            return result;
        }

        public void ProcessMessage(MqttMessage mqttMessage)
        {
            SensorHost sensorHost;

            if (sensorTopics.TryGetValue(mqttMessage.GetRawTopic(), out sensorHost))
            {
                Log.Trace($"[NW] Sending message for topic [{mqttMessage.GetRawTopic()}] to [{sensorHost.SensorIdentifier}]");
                sensorHost.sensor.ProcessMessage(mqttMessage);

                // Found a basic topic, no need to search the wildcards
                return;
            }

            foreach (var item in sensorMultiLevelWildcardTopics)
            {
                var wildcardTopic = item.Key.Split("/");
                var messageTopic = mqttMessage.GetRawTopic().Split("/");

                if (messageTopic.Count() < wildcardTopic.Count())
                    continue;

                bool wildcardFound = false;
                for (int i = 0; i < wildcardTopic.Count(); i++)
                {
                    
                    if (!wildcardFound && i < wildcardTopic.Count())
                    {

                        if (wildcardTopic[i] == "#")
                            wildcardFound = true;

                        if ((messageTopic[i] != wildcardTopic[i]) && !wildcardFound)
                            break;
                    }

                    if (wildcardFound)
                    {
                        Log.Trace($"[MW] Sending message for topic [{mqttMessage.GetRawTopic()}] to [{item.Value.SensorIdentifier}]");
                        item.Value.sensor.ProcessMessage(mqttMessage);
                        return;
                    }

                }

            }


            foreach (var item in sensorSingleLevelWildcardTopics)
            {
                var wildcardTopic = item.Key.Split("/");
                var messageTopic = mqttMessage.GetRawTopic().Split("/");

                if (messageTopic.Count() != wildcardTopic.Count())
                    continue;

                for (int i = 0; i < messageTopic.Count(); i++)
                {

                    if ((messageTopic[i] != wildcardTopic[i]) && (wildcardTopic[i] != "+"))
                        break;

                    if (i == wildcardTopic.Count() - 1)
                    {
                        Log.Trace($"[SW] Sending message for topic [{mqttMessage.GetRawTopic()}] to [{item.Value.SensorIdentifier}]");
                        item.Value.sensor.ProcessMessage(mqttMessage);
                        return;
                    }

                }

            }
        }

        public void UnmapAlltopics(SensorHost sensorHost)
        {
            Log.Debug($"Unmapping and unsubscribing to all topics for [{sensorHost.SensorIdentifier}]");

            foreach (var item in sensorTopics)
            {
                if (item.Value == sensorHost)
                {

                    var mTopic = MqttMessageBuilder
                        .NewMessage()
                        .AddTopic(item.Key)
                        .UnsubscribeMessage
                        .Build();

                    Log.Debug($"Unsubscribing from [{item.Key}] for [{item.Value.sensor.GetSensorIdentifier()}]");
                    _client.Unsubscribe(mTopic);
                    sensorTopics.TryRemove(item.Key, out var removed);
                }
            }

            foreach (var item in sensorMultiLevelWildcardTopics)
            {
                if (item.Value == sensorHost)
                {

                    var mMulti = MqttMessageBuilder
                        .NewMessage()
                        .AddTopic(item.Key)
                        .UnsubscribeMessage
                        .Build();

                    Log.Debug($"Unsubscribing from [{item.Key}] for [{item.Value.sensor.GetSensorIdentifier()}]");
                    _client.Unsubscribe(mMulti);

                    sensorMultiLevelWildcardTopics.Remove(item.Key, out var removed);
                }
            }

            foreach (var item in sensorSingleLevelWildcardTopics)
            {
                if (item.Value == sensorHost)
                {
                    
                    var mSingle = MqttMessageBuilder
                        .NewMessage()
                        .AddTopic(item.Key)
                        .UnsubscribeMessage
                        .Build();

                    Log.Debug($"Unsubscribing from [{item.Key}] for [{item.Value.sensor.GetSensorIdentifier()}]");
                    _client.Unsubscribe(mSingle);
                    sensorSingleLevelWildcardTopics.Remove(item.Key, out var removed);
                }
            }
        }

        public bool UnmapTopicToSensor(MqttMessage mqttMessage, SensorHost sensorHost)
        {
            Log.Debug($"Unmapping topic [{mqttMessage.GetRawTopic()}] for [{sensorHost.SensorIdentifier}]");

            SensorHost previous;
            bool result;

            if (mqttMessage.GetRawTopic().Contains("/#")) // multi-level wildcard topic
            {

                    sensorMultiLevelWildcardTopics.TryRemove(mqttMessage.GetRawTopic().Replace("/#", ""), out var sMultiWild);
                result = true;
            }
            else if (mqttMessage.GetRawTopic().Contains("/+")) // single level wildcard topic
            {

                    sensorSingleLevelWildcardTopics.TryRemove(mqttMessage.GetRawTopic().Replace("/+", ""), out var sSingleWild);
                result = true;
            }
            else
            {
                result = sensorTopics.TryRemove(mqttMessage.GetRawTopic(), out previous);

                if (sensorHost != previous)
                    Log.Warn($"{sensorHost.SensorIdentifier} unsubscribed for [{previous.SensorIdentifier}]");
            }

            return result;
        }

        internal List<string> LoadBuiltInScripts()
        {
            System.Reflection.Assembly ass = System.Reflection.Assembly.GetEntryAssembly();


            List<string> availableSensors = new List<string>();

            var tasks = new List<Task<SensorHost>>();

            foreach (System.Reflection.TypeInfo ti in ass.DefinedTypes)
            {
                if (ti.ImplementedInterfaces.Contains(typeof(ISensor)))
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var s = new SensorHost((ISensor)ass.CreateInstance(ti.FullName), _client, this);

                        if (s != null && !this.sensors.TryAdd(s.SensorIdentifier, s))
                            Log.Debug($"Skipping built-in sensor [{s.SensorIdentifier}]");

                        return s;
                    }));
                }
            }
            Task.WaitAll(tasks.ToArray());

            availableSensors.AddRange(tasks.Select(item => item.Result.SensorIdentifier).Where(ident => ident != null));

            return availableSensors;

        }

        internal void StartSensors()
        {
            foreach (var item in sensors)
            {
                _ = Task.Run(() =>
                    {
                        while (item.Value.IsCompiled && !item.Value.sensor.IsInitialized)
                            Thread.Sleep(10);

                        if (item.Value != null && item.Value.IsCompiled && item.Value.sensor.IsInitialized)
                        {
                            Log.Trace($"StartSensor sensor: [{item.Value.SensorIdentifier}]");
                            item.Value.sensor.SensorMain();
                        }
                        else
                        {
                            Log.Trace("Sensor is null, not compiled, or uninitialized. Skipping.");
                        }
                    });
            }

            sensorCleanupTimer.Start();
        }

        private SensorHost LoadSensor(string filePath)
        {
            var s = new SensorHost(_client, this);
            s.LoadFromFile(filePath);

            if (s.IsCodeLoaded && s.IsCompiled)
            {
                if (!this.sensors.TryAdd(s.SensorIdentifier, s))
                    Log.Debug($"Unable to load sensor [{s.SensorIdentifier}] - same sensor name exists?");
                else
                    Log.Debug($"Found and loaded sensor [{s.SensorIdentifier}]");
            }
            else
            {
                Log.Warn($"Unable to load/compile {filePath}: [{s.GetLastError}]");
            }

            return s;
        }

        private void SensorCleanupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Log.Trace("Starting a clean of uncompiled sensors");


            foreach (var item in sensors)
            {
                if (!item.Value.IsCompiled || !item.Value.sensor.IsInitialized)
                {
                    Log.Info($"Removing unused sensor [{item.Value.SensorIdentifier}]");
                    item.Value.Dispose();
                    sensors.Remove(item.Key, out var s);
                    DisposeSensorHost(item.Value);
                    var before = System.GC.GetTotalMemory(false);
                    var after = System.GC.GetTotalMemory(true);

                    Log.Debug($"Recovered {(before - after).ToReadableFileSize()} of memory.");
                }
            }
        }
    }
}