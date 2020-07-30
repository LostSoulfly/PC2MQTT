using PC2MQTT.Helpers;
using PC2MQTT.Sensors;
using System;

namespace PC2MQTT
{
    internal class Program
    {
        public static Settings settings = new Settings();
        public static MQTT.Client client;
        public static SensorManager sensorManager;
        public static readonly string Version = "0.1.0-dev";

        private static void Main(string[] args)
        {
            Console.WriteLine($"PC2MQTT v{Version} starting");
            InitializeSettings();
            InitializeMqtt();
            InitializeSensors();

            Console.WriteLine("Press Escape to exit.");
            while(Console.ReadKey().Key != ConsoleKey.Escape) { }
        }

        private static void InitializeSensors()
        {
            // Initialize sensor handlers and map topics for them

            sensorManager = new SensorManager(client, settings);

            var available = sensorManager.LoadSensorScripts();


            if (settings.config.enabledSensors.Count == 0)
            {
                Logging.Log("No sensors enabled, enabling all found sensors..");
                settings.config.enabledSensors = available;
                settings.SaveSettings();
            }

            sensorManager.InitializeSensors(settings.config.enabledSensors);

        }

        private static void InitializeMqtt()
        {

            if (settings.config.mqttSettings.broker.Length == 0 || settings.config.mqttSettings.port == 0)
            {
                Logging.Log("Unable to initialized MQTT, missing connection details!");
                Environment.Exit(1);
            }

            Logging.Log($"Initializing MQTT client..");
            client = new MQTT.Client(settings.config.mqttSettings, true);

            client.ConnectionClosed += Client_ConnectionClosed;
            client.ConnectionConnected += Client_ConnectionConnected;
            client.MessagePublished += Client_MessagePublished;
            client.MessageReceivedString += Client_MessageReceivedString;

            client.MqttConnect();
        }

        private static void Client_MessageReceivedString(string message, string topic)
        {
            Logging.Log($"Message received for topic [{topic}] {message}");
        }

        private static void Client_MessagePublished(ushort messageId, bool isPublished)
        {

            //Logging.Log($"Message published ({isPublished}): {messageId}");
        }

        private static void Client_ConnectionConnected()
        {
            Logging.Log($"Connected to MQTT server {settings.config.mqttSettings.broker}");
        }

        private static void Client_ConnectionClosed(string reason, byte errorCode)
        {

            Logging.Log($"Connection toMQTT server {settings.config.mqttSettings.broker} closed:: {reason}");
        }

        private static void InitializeSettings()
        {
            if (!settings.LoadSettings())
            {
                settings.SaveSettings();
                Logging.Log("Generating default settings. Please edit config.json and re-launch the program.");
                Environment.Exit(0);
            }
        }
    }
}