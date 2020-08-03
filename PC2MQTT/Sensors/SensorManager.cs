using BadLogger;
using ExtensionMethods;
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

            Log = LogManager.GetCurrentClassLogger();
            sensorCleanupTimer = new System.Timers.Timer(60000);
            sensorCleanupTimer.Elapsed += SensorCleanupTimer_Elapsed;

            sensorTopics = new ConcurrentDictionary<string, SensorHost>();
            sensorMultiLevelWildcardTopics = new ConcurrentDictionary<string, SensorHost>();
            sensorSingleLevelWildcardTopics = new ConcurrentDictionary<string, SensorHost>();
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

        public bool MapTopicToSensor(string topic, SensorHost sensorHost, bool prependDeviceId = true)
        {
            Log.Debug($"Mapping topic [{topic.ResultantTopic(prependDeviceId)}] for [{sensorHost.SensorIdentifier}]");

            bool result;

            if (topic.Contains("/#")) // multi-level wildcard topic
            {
                result = sensorMultiLevelWildcardTopics.TryAdd(topic.ResultantTopic(prependDeviceId, true), sensorHost);

            }
            else if (topic.Contains("/+"))// single level wildcard topic
            {
                result = sensorSingleLevelWildcardTopics.TryAdd(topic.ResultantTopic(prependDeviceId, true), sensorHost);
            }
            else
            {
                result = sensorTopics.TryAdd(topic.ResultantTopic(prependDeviceId), sensorHost);
            }

            return result;
        }

        public void ProcessMessage(string topic, string message)
        {
            SensorHost sensorHost;

            if (sensorTopics.TryGetValue(topic, out sensorHost))
            {
                Log.Trace($"Sending message for topic [{topic}] to [{sensorHost.SensorIdentifier}]");
                sensorHost.sensor.ProcessMessage(topic.RemoveDeviceId(), message);

                // Found a basic topic, no need to search the wildcards
                return;
            }

                foreach (var item in sensorMultiLevelWildcardTopics)
                {
                    if (topic == item.Key || topic.Contains(item.Key + "/"))
                    {
                        Log.Trace($"Sending message for topic [{topic}] to [{item.Value.SensorIdentifier}]");
                        item.Value.sensor.ProcessMessage(topic.RemoveDeviceId(), message);
                        return;
                    }
                }

                foreach (var item in sensorSingleLevelWildcardTopics)
                {
                    if (topic == item.Key || !topic.Substring(item.Key.Length + 1).Contains("/"))
                    {
                        Log.Trace($"Sending message for topic [{topic}] to [{item.Value.SensorIdentifier}]");
                        item.Value.sensor.ProcessMessage(topic.RemoveDeviceId(), message);
                        return;
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
                    Log.Debug($"Unsubscribing from [{item.Key}] for [{item.Value.sensor.GetSensorIdentifier()}]");
                    _client.Unubscribe(item.Key, false);
                    sensorTopics.TryRemove(item.Key, out var removed);
                }
            }

                foreach (var item in sensorMultiLevelWildcardTopics)
                {
                    if (item.Value == sensorHost)
                    {
                        Log.Debug($"Unsubscribing from [{item.Key}/#] for [{item.Value.sensor.GetSensorIdentifier()}]");
                        _client.Unubscribe(item.Key + "/#", false);
                        sensorMultiLevelWildcardTopics.Remove(item.Key, out var removed);
                    }
                }

                foreach (var item in sensorSingleLevelWildcardTopics)
                {
                    if (item.Value == sensorHost)
                    {
                        Log.Debug($"Unsubscribing from [{item.Key}/+] for [{item.Value.sensor.GetSensorIdentifier()}]");
                        _client.Unubscribe(item.Key + "/+", false);
                        sensorSingleLevelWildcardTopics.Remove(item.Key, out var removed);
                    }
                }
        }

        public bool UnmapTopicToSensor(string topic, SensorHost sensorHost)
        {
            Log.Debug($"Unmapping topic [{topic}] for [{sensorHost.SensorIdentifier}]");

            SensorHost previous;
            bool result;

            if (topic.Contains("/#")) // multi-level wildcard topic
            {

                    sensorMultiLevelWildcardTopics.TryRemove(topic.Replace("/#", ""), out var sMultiWild);
                result = true;
            }
            else if (topic.Contains("/+")) // single level wildcard topic
            {

                    sensorSingleLevelWildcardTopics.TryRemove(topic.Replace("/+", ""), out var sSingleWild);
                result = true;
            }
            else
            {
                result = sensorTopics.TryRemove(topic, out previous);

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
                //Task<SensorHost> t;
                //t = Task.Run(() => LoadSensor(item));
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