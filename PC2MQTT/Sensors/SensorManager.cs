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
        public List<SensorHost> sensors = new List<SensorHost>();
        private IClient _client;

        private object _dictMultiLock = new object();
        private object _dictSingleLock = new object();
        private object _sensorListLock = new object();

        private Helpers.Settings _settings;
        private BadLogger.BadLogger Log;
        private System.Timers.Timer sensorCleanupTimer;
        private Dictionary<string, SensorHost> sensorMultiLevelWildcardTopics;
        private Dictionary<string, SensorHost> sensorSingleLevelWildcardTopics;
        private ConcurrentDictionary<string, SensorHost> sensorTopics;

        public SensorManager(IClient client, Helpers.Settings settings)
        {
            this._client = client;
            this._settings = settings;

            Log = LogManager.GetCurrentClassLogger();
            sensorCleanupTimer = new System.Timers.Timer(60000);
            sensorCleanupTimer.Elapsed += SensorCleanupTimer_Elapsed;

            sensorTopics = new ConcurrentDictionary<string, SensorHost>();
            sensorMultiLevelWildcardTopics = new Dictionary<string, SensorHost>();
            sensorSingleLevelWildcardTopics = new Dictionary<string, SensorHost>();
        }

        public void Dispose()
        {
            Log.Info("Disposing of SensorManager..");

            lock (_sensorListLock)
            {
                for (int i = sensors.Count - 1; i > -1; i--)
                {
                    SensorHost sensor = sensors[i];
                    if (sensors[i].IsCompiled)
                    {
                        Log.Debug($"Disposing sensor [{sensor.SensorIdentifier}]");
                        sensor.Dispose();
                        sensors.Remove(sensor);
                        DisposeSensorHost(sensor);
                    }
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
            lock (_sensorListLock)
            {
                foreach (var item in sensors)
                {
                    Task.Run(() =>
                    {
                        if ((enabledSensors.Contains("*") || enabledSensors.Contains(item.SensorIdentifier)) && item.InitializeSensor())
                        {
                            Log.Info($"Initialized sensor: [{item.SensorIdentifier}]");
                        }
                        else
                        {
                            Log.Debug($"Skipping sensor: [{item.SensorIdentifier}]");
                        }
                    });
                }
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

            availableSensors.AddRange(tasks.Select(item => item.Result.SensorIdentifier));

            return availableSensors;
        }

        public bool MapTopicToSensor(string topic, SensorHost sensorHost, bool prependDeviceId = true)
        {
            Log.Debug($"Mapping topic [{topic.ResultantTopic(prependDeviceId)}] for [{sensorHost.SensorIdentifier}]");

            bool result;

            if (topic.Contains("/#")) // multi-level wildcard topic
            {
                lock (_dictMultiLock)
                    sensorMultiLevelWildcardTopics.Add(topic.ResultantTopic(prependDeviceId, true), sensorHost);

                result = true;
            }
            else if (topic.Contains("/+"))// single level wildcard topic
            {
                lock (_dictSingleLock)
                    sensorSingleLevelWildcardTopics.Add(topic.ResultantTopic(prependDeviceId, true), sensorHost);
                result = true;
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

            lock (_dictMultiLock)
            {
                foreach (var item in sensorMultiLevelWildcardTopics)
                {
                    if (topic.Contains(item.Key))
                    {
                        Log.Trace($"Sending message for topic [{topic}] to [{item.Value.SensorIdentifier}]");
                        item.Value.sensor.ProcessMessage(topic.RemoveDeviceId(), message);
                        return;
                    }
                }
            }

            lock (_dictSingleLock)
            {
                foreach (var item in sensorSingleLevelWildcardTopics)
                {
                    if (topic.Contains(item.Key))
                    {
                        Log.Trace($"Sending message for topic [{topic}] to [{item.Value.SensorIdentifier}]");
                        item.Value.sensor.ProcessMessage(topic.RemoveDeviceId(), message);
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
                    Log.Debug($"Unsubscribing from [{item.Key}] for [{item.Value.sensor.GetSensorIdentifier()}]");
                    _client.Unubscribe(item.Key, false);
                    sensorTopics.TryRemove(item.Key, out var removed);
                }
            }

            lock (_dictMultiLock)
            {
                foreach (var item in sensorMultiLevelWildcardTopics)
                {
                    if (item.Value == sensorHost)
                    {
                        Log.Debug($"Unsubscribing from [{item.Key}/#] for [{item.Value.sensor.GetSensorIdentifier()}]");
                        _client.Unubscribe(item.Key + "/#", false);
                        sensorMultiLevelWildcardTopics.Remove(item.Key, out var removed);
                    }
                }
            }

            lock (_dictSingleLock)
            {
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
        }

        public bool UnmapTopicToSensor(string topic, SensorHost sensorHost)
        {
            Log.Debug($"Unmapping topic [{topic}] for [{sensorHost.SensorIdentifier}]");

            SensorHost previous;
            bool result;

            if (topic.Contains("/#")) // multi-level wildcard topic
            {
                lock (_dictMultiLock)
                    sensorMultiLevelWildcardTopics.Remove(topic.Replace("/#", ""));
                result = true;
            }
            else if (topic.Contains("/+")) // single level wildcard topic
            {
                lock (_dictSingleLock)
                    sensorSingleLevelWildcardTopics.Remove(topic.Replace("/+", ""));
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

        internal void LoadBuiltInScripts()
        {
            System.Reflection.Assembly ass = System.Reflection.Assembly.GetEntryAssembly();

            foreach (System.Reflection.TypeInfo ti in ass.DefinedTypes)
            {
                if (ti.ImplementedInterfaces.Contains(typeof(ISensor)))
                {
                    Task.Run(() =>
                    {
                        lock (_sensorListLock)
                        {
                            this.sensors.Add(new SensorHost((ISensor)ass.CreateInstance(ti.FullName), _client, this));
                        }
                    });
                }
            }
        }

        internal void StartSensors()
        {
            lock (_sensorListLock)
            {
                foreach (var item in sensors)
                {
                    //Task<SensorHost> t;
                    //t = Task.Run(() => LoadSensor(item));
                    _ = Task.Run(() =>
                      {
                          while (item.IsCompiled && !item.sensor.IsInitialized)
                              Thread.Sleep(10);

                          if (item != null && item.IsCompiled && item.sensor.IsInitialized)
                          {
                              Log.Trace($"StartSensor sensor: [{item.SensorIdentifier}]");
                              item.sensor.SensorMain();
                          }
                          else
                          {
                              Log.Trace("Sensor is null, not compiled, or uninitialized. Skipping.");
                          }
                      });
                }
            }

            sensorCleanupTimer.Start();
        }

        private SensorHost LoadSensor(string filePath)
        {
            var s = new SensorHost(_client, this);
            s.LoadFromFile(filePath);

            if (s.IsCodeLoaded && s.IsCompiled)
            {
                lock (_sensorListLock)
                {
                    this.sensors.Add(s);
                }

                Log.Debug($"Found and loaded sensor: [{s.SensorIdentifier}]");
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
            lock (_sensorListLock)
            {
                for (int i = sensors.Count - 1; i > -1; i--)
                {
                    SensorHost sensor = sensors[i];
                    if (!sensors[i].IsCompiled || !sensors[i].sensor.IsInitialized)
                    {
                        Log.Info($"Removing unused sensor [{sensor.SensorIdentifier}]");
                        sensor.Dispose();
                        sensors.Remove(sensor);
                        DisposeSensorHost(sensor);
                        var before = System.GC.GetTotalMemory(false);
                        var after = System.GC.GetTotalMemory(true);

                        Log.Debug($"Recovered {(before - after).ToReadableFileSize()} of memory.");
                    }
                }
            }
        }
    }
}