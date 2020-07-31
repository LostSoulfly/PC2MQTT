using BadLogger;
using CSScriptLib;
using ExtensionMethods;
using PC2MQTT.Helpers;
using PC2MQTT.MQTT;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

namespace PC2MQTT.Sensors
{
    public class SensorManager
    {

        public List<SensorHost> sensors = new List<SensorHost>();
        private ConcurrentDictionary<string, SensorHost> sensorTopics;
        private Dictionary<string, SensorHost> sensorMultiLevelWildcardTopics;
        private Dictionary<string, SensorHost> sensorSingleLevelWildcardTopics;

        // Lock objects to be safe, regular dictionry isn't threadsafe
        private object _dictSingleLock = new object();
        private object _dictMultiLock = new object();

        private BadLogger.BadLogger Log;
        Client _client;
        Helpers.Settings _settings;

        Timer sensorCleanupTimer;

        public SensorManager(Client client, Helpers.Settings settings)
        {
            this._client = client;
            this._settings = settings;
            Log = LogManager.GetCurrentClassLogger();
            sensorCleanupTimer = new Timer(60000);
            sensorCleanupTimer.Elapsed += SensorCleanupTimer_Elapsed;

            sensorTopics = new ConcurrentDictionary<string, SensorHost>();
            sensorMultiLevelWildcardTopics = new Dictionary<string, SensorHost>();
            sensorSingleLevelWildcardTopics = new Dictionary<string, SensorHost>();
        }

        private void SensorCleanupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Log.Trace("Starting a clean of uncompiled sensors");
            for (int i = sensors.Count -1; i > -1; i--)
            {
                SensorHost sensor = sensors[i];
                if (!sensors[i].IsCompiled || !sensors[i].sensor.IsInitialized)
                {
                    Log.Trace($"Removing unused sensor [{sensor.SensorIdentifier}]");
                    sensor.Dispose();
                    sensors.Remove(sensor);
                    DisposeSensorHost(sensor);
                    var before = System.GC.GetTotalMemory(false);
                    var after= System.GC.GetTotalMemory(true);

                    Log.Debug($"Recovered {(before - after).ToReadableFileSize()} of memory.");
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

            var sensorFiles = Directory.GetFiles("sensors/", "*.cs").ToList();

            foreach (var item in sensorFiles)
            {
                Log.Trace("Compiling script files, this may take a moment..");
                var s = new SensorHost(_settings.config.mqttSettings.deviceId, _client, this);
                s.LoadFromFile(item);

                if (s.IsCodeLoaded && s.IsCompiled)
                {
                    this.sensors.Add(s);

                    Log.Debug($"Found and loaded sensor: [{s.SensorIdentifier}]");
                    availableSensors.Add(s.SensorIdentifier);

                }
                else
                {
                    Log.Warn($"Unable to load/compile {item}: [{s.GetLastError}]");
                }
            }

            return availableSensors;
        }

        public int InitializeSensors(List<string> enabledSensors)
        {
            int initializedCount = 0;

            foreach (var item in sensors)
            {
                if (enabledSensors.Contains(item.SensorIdentifier) && item.InitializeSensor())
                {
                    Log.Info($"Initialized sensor: [{item.SensorIdentifier}]");
                    initializedCount++;
                } else
                {

                    Log.Debug($"Skipping sensor: [{item.SensorIdentifier}]");
                }
            }

            sensorCleanupTimer.Start();

            return initializedCount;
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

            lock (_dictSingleLock) {
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
        public void DisposeSensorHost(SensorHost sensorHost)
        {
            sensorHost.Dispose();
            sensorHost = null;
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
    }
}
