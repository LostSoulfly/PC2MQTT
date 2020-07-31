using BadLogger;
using PC2MQTT.Helpers;
using PC2MQTT.MQTT;
using PC2MQTT.Sensors;
using System;

namespace PC2MQTT
{
    internal class Program
    {
        public static readonly string Version = "0.1.0-dev";
        public static IClient client;
        public static SensorManager sensorManager;
        public static Settings settings = new Settings();
        private static BadLogger.BadLogger Log;

        private static void Client_ConnectionClosed(string reason, byte errorCode)
        {
            Log.Warn($"Connection to MQTT server {settings.config.mqttSettings.broker} closed: {reason}");
        }

        private static void Client_ConnectionConnected()
        {
            Log.Info($"Connected to MQTT server {settings.config.mqttSettings.broker}");
        }

        private static void Client_MessagePublished(string topic, string message)
        {
        }

        private static void Client_MessageReceivedString(string topic, string message)
        {
            Log.Trace($"Message received for [{topic}]: {message}");
            sensorManager.ProcessMessage(topic, message);
        }

        private static void Client_TopicSubscribed(string topic)
        {
        }

        private static void Client_TopicUnsubscribed(string topic)
        {
        }

        private static void InitializeExtensions()
        {
            ExtensionMethods.Extensions.deviceId = settings.config.mqttSettings.deviceId;
        }

        private static void InitializeMqtt()
        {
            if (settings.config.mqttSettings.broker.Length == 0 || settings.config.mqttSettings.port == 0)
            {
                Log.Fatal("Unable to initialized MQTT, missing connection details!");
                Environment.Exit(1);
            }

            Log.Debug($"Initializing MQTT client..");

            if (true)
                client = new FakeClient(settings.config.mqttSettings);
            else
                client = new MQTT.Client(settings.config.mqttSettings, true);

            client.ConnectionClosed += Client_ConnectionClosed;
            client.ConnectionConnected += Client_ConnectionConnected;
            client.MessagePublished += Client_MessagePublished;
            client.TopicSubscribed += Client_TopicSubscribed;
            client.MessageReceivedString += Client_MessageReceivedString;
            client.TopicUnsubscribed += Client_TopicUnsubscribed;

            client.MqttConnect();
        }

        private static void InitializeSensors()
        {
            // Initialize sensor handlers and map topics for them

            sensorManager = new SensorManager(client, settings);

            var available = sensorManager.LoadSensorScripts();

            if (settings.config.enabledSensors.Count == 0)
            {
                Log.Info("No sensors enabled, enabling all found sensors..");
                settings.config.enabledSensors = available;
                settings.SaveSettings();
            }

            var loaded = sensorManager.InitializeSensors(settings.config.enabledSensors);

            Log.Info($"Loaded {loaded} out of {available.Count} sensors.");
        }

        private static void InitializeSettings()
        {
            if (!settings.LoadSettings())
            {
                settings.SaveSettings();
                Console.WriteLine("Generating default settings. Please edit config.json and re-launch the program.");
                Environment.Exit(0);
            }
        }

        private static void Main(string[] args)
        {
            Console.WriteLine($"PC2MQTT v{Version} starting");

            InitializeSettings();
            InitializeExtensions();

            Logging.InitializeLogging(settings);
            Log = LogManager.GetCurrentClassLogger();

            InitializeMqtt();
            InitializeSensors();

            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                System.Threading.Thread.Sleep(1);
            }

            Environment.Exit(0);
        }
    }
}