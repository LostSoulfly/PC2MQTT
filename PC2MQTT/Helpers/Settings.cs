using Newtonsoft.Json;
using PC2MQTT.MQTT;
using PC2MQTT.Sensors;
using System;
using System.Collections.Generic;
using System.IO;

namespace PC2MQTT.Helpers
{
    public class Config
    {
        // no ssl support cause i ain't use it :(

        public bool minimizeAtLaunch = true;
        public bool enableLogging = true;
        public bool logToConsole = true;
        public MqttSettings mqttSettings = new MqttSettings();
        public List<string> enabledSensors = new List<string>();

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
                    return true;
                }

            }
            catch (Exception ex)
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
            catch (Exception ex)
            {
                //Logging.Log($"Unable to save settings file {fileName}: {ex.Message}");
            }

            return false;
        }
    }
}