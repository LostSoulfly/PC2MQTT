using CSScriptLib;
using PC2MQTT.Helpers;
using PC2MQTT.MQTT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PC2MQTT.Sensors
{
    public class SensorManager
    {

        public List<SensorHost> sensors = new List<SensorHost>();

        Client _client;
        Helpers.Settings _settings;

        public SensorManager(Client client, Helpers.Settings settings)
        {
            this._client = client;
            this._settings = settings;
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
                Logging.Log("Compiling script files, this may take a moment..");
                var s = new SensorHost(_settings.config.mqttSettings.deviceId, _client);
                s.LoadFromFile(item);

                if (s.IsCodeLoaded && s.IsCompiled)
                {
                    this.sensors.Add(s);

                    Logging.Log($"Found and loaded sensor: {s.SensorIdentifier}");
                    availableSensors.Add(s.SensorIdentifier);

                }
                else
                {
                    Helpers.Logging.Log($"Unable to load/compile {item}: {s.GetLastError}");
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
                    Logging.Log($"Initialized sensor: {item.SensorIdentifier}");
                    initializedCount++;
                } else
                {

                    Logging.Log($"Skipping sensor: {item.SensorIdentifier}");
                }
            }

            return initializedCount;
        }

    }
}
