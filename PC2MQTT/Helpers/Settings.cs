using Newtonsoft.Json;
using PC2MQTT.MQTT;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace PC2MQTT.Helpers
{
    public class Config
    {
        // no ssl support cause i ain't use it :(

        public List<string> enabledSensors = new List<string>();
        public bool enableLogging = true;
        public string logLevel = "INFO";
        public bool logToConsole = true;
        public bool minimizeAtLaunch = true;
        public MqttSettings mqttSettings = new MqttSettings();
        public bool useOnlyBuiltInSensors = true;
        public bool resubscribeOnReconnect = true;
        public ConcurrentDictionary<string, string> sensorData = new ConcurrentDictionary<string, string>();

    }

    public class Settings
    {
        public Config config;
        public string configFileName = "config.json";
        System.Timers.Timer autoSaveTimer;
        public bool newDataToSave = false;

        public bool LoadSettings(string fileName = "")
        {
            this.config = new Config();
            if (String.IsNullOrWhiteSpace(fileName))
                fileName = configFileName;

            StartAutoSaveTimer();

            try
            {
                var loaded = JsonConvert.DeserializeObject<Config>(File.ReadAllText(fileName));

                if (loaded != null)
                {
                    config = loaded;
                    config.mqttSettings.deviceId = config.mqttSettings.deviceId.ToLower();
                    if (config.sensorData == null) config.sensorData = new ConcurrentDictionary<string, string>();
                    return true;
                }
            }
            catch { }

            return false;
        }

        public bool SaveSettings(string fileName = "")
        {
            try
            {
                if (String.IsNullOrWhiteSpace(fileName))
                    fileName = configFileName;

                File.WriteAllText(fileName, JsonConvert.SerializeObject(config, formatting: Formatting.Indented));
                return true;
            }
            catch { }

            return false;
        }

        private void StartAutoSaveTimer()
        {
            if (autoSaveTimer != null)
                return;

            autoSaveTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
            autoSaveTimer.Elapsed += delegate {
                if (this.newDataToSave)
                    this.SaveSettings();
            };
            autoSaveTimer.Start();
        }
    }
}