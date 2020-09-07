using Newtonsoft.Json;
using PC2MQTT.MQTT;
using System;
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
        public bool useOnlyBuiltInScripts = true;
        //public int screenshotServerPort = 8081;
        //public int webcamServerPort = 8080;
        //public webcamToStream;
    }

    public class Settings
    {
        public Config config;
        public string configFileName = "config.json";

        public bool LoadSettings(string fileName = "")
        {
            this.config = new Config();
            if (String.IsNullOrWhiteSpace(fileName))
                fileName = configFileName;

            try
            {
                var loaded = JsonConvert.DeserializeObject<Config>(File.ReadAllText(fileName));

                if (loaded != null)
                {
                    config = loaded;
                    config.mqttSettings.deviceId = config.mqttSettings.deviceId.ToLower();
                    return true;
                }
            }
            catch
            {
                //Logging.Log($"Unable to load settings file {fileName}: {ex.Message}");
            }

            return false;
        }

        public bool SaveSettings(string fileName = "")
        {
            //Logging.Log($"Saving settings to {fileName}..");
            try
            {
                if (String.IsNullOrWhiteSpace(fileName))
                    fileName = configFileName;

                File.WriteAllText(fileName, JsonConvert.SerializeObject(config, formatting: Formatting.Indented));
                return true;
            }
            catch
            {
                //Logging.Log($"Unable to save settings file {fileName}: {ex.Message}");
            }

            return false;
        }
    }
}