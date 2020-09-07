using BadLogger;
using CSScriptLib;
using ExtensionMethods;
using PC2MQTT.MQTT;
using System;
using System.IO;
using System.Reflection;

namespace PC2MQTT.Sensors
{
    public class SensorHost : IDisposable
    {
        public string code { get; private set; }
        public string GetLastError { get; private set; }
        public bool IsCodeLoaded { get; private set; }
        public bool IsCompiled { get; private set; }
        public ISensor sensor { get; private set; }
        public string SensorIdentifier { get; private set; }
        private IClient _client;
        private bool _hasBeendisposed;

        private SensorManager _sensorManager;
        private BadLogger.BadLogger Log;

        public SensorHost(string code, IClient client, SensorManager sensorManager)
        {
            this.code = code;
            this._client = client;
            this._sensorManager = sensorManager;

            Log = LogManager.GetCurrentClassLogger();

            IsCodeLoaded = true;

            Compile();
        }

        public SensorHost(IClient client, SensorManager sensorManager)
        {
            this.code = code;
            this._client = client;
            this._sensorManager = sensorManager;

            Log = LogManager.GetCurrentClassLogger();

            IsCodeLoaded = false;
        }

        public SensorHost(ISensor sensor, IClient client, SensorManager sensorManager)
        {
            this._client = client;
            this._sensorManager = sensorManager;
            this.sensor = sensor;

            Log = LogManager.GetCurrentClassLogger();

            if (sensor.IsCompatibleWithCurrentRuntime())
            {
                IsCodeLoaded = true;
                IsCompiled = true;
            }

            this.SensorIdentifier = sensor.GetSensorIdentifier().ToLower();
        }

        public void Dispose()
        {
            DisposeSensor();
            GC.SuppressFinalize(this);
        }

        public void DisposeSensor()
        {
            if (_hasBeendisposed) return;

            _sensorManager.UnmapAlltopics(this);
            try
            {

                if (sensor != null)
                {
                    UninitializeSensor();
                    sensor.Dispose();
                }
            }
            catch (Exception ex) { Log.Warn($"Unable to properly dispose of {sensor.GetSensorIdentifier()}: {ex.Message}"); }

            code = null;
            IsCodeLoaded = false;
            IsCompiled = false;
            this._client = null;
            //this.SensorIdentifier = null;
            this.GetLastError = null;
            this.sensor = null;

            _hasBeendisposed = true;
        }

        public bool InitializeSensor()
        {
            if (IsCompiled)
                sensor.IsInitialized = sensor.Initialize(this);

            return sensor.IsInitialized;
        }

        public void LoadFromFile(string filePath, bool compile = true)
        {
            try
            {
                this.code = File.ReadAllText(filePath);
            }
            catch (Exception ex) { GetLastError = ex.Message; }

            IsCodeLoaded = true;
            if (compile) Compile();

            if (this.SensorIdentifier == null)
                this.SensorIdentifier = Path.GetFileName(filePath);
        }

        public bool Publish(MqttMessage mqttMessage)
        {
            if (_client == null)
                return false;

            Log.Trace($"[{SensorIdentifier}] publishing to [{mqttMessage.GetRawTopic()}]: [{mqttMessage.message}]");

            var success = _client.Publish(mqttMessage);

            if (success.messageId > 0) return true;

            return false;
        }

        public void UninitializeSensor()
        {
            if (sensor != null && IsCompiled && sensor.IsInitialized)
            {
                sensor.Uninitialize();
                sensor.IsInitialized = false;
            }
        }

        public bool Unsubscribe(MqttMessage mqttMessage)
        {
            if (_client == null)
                return false;

            var success = _client.Unsubscribe(mqttMessage);
            Log.Trace($"[{SensorIdentifier}] unsubscribing to [{mqttMessage.GetRawTopic()}] ({success})");

            if (success.messageId > 0)
            {
                _sensorManager.UnmapTopicToSensor(mqttMessage, this);
                return true;
            }

            return false;
        }

        public bool Subscribe(MqttMessage mqttMessage)
        {
            if (_client == null)
                return false;

            var success = _client.Subscribe(mqttMessage);
            Log.Trace($"[{SensorIdentifier}] subscribing to [{mqttMessage.GetRawTopic()}]");

            if (success.messageId > 0)
            {
                _sensorManager.MapTopicToSensor(mqttMessage, this);

                return true;
            }

            return false;
        }

        public void UnsubscribeAllTopics() => _sensorManager.UnmapAlltopics(this);

        private void Compile()
        {
            if (!IsCodeLoaded)
                return;

            if (!this.code.Contains("using PC2MQTT.MQTT;"))
                this.code = "using PC2MQTT.MQTT;\r\n" + this.code;

            if (!this.code.Contains("PC2MQTT.Helpers;"))
                this.code = "using PC2MQTT.Helpers;\r\n" + this.code;

            if (!this.code.Contains("using PC2MQTT.Sensors;"))
                this.code = "using PC2MQTT.Sensors;\r\n" + this.code;

            if (!this.code.Contains("using System;"))
                this.code = "using System;\r\n" + this.code;

            string ns = "namespace PC2MQTT.Sensors";

            if (this.code.Contains(ns))
            {
                Log.Trace("Sensor script contains a namespace. Attempting to remove it..");

                var nsLocation = this.code.IndexOf(ns);
                this.code = this.code.Remove(nsLocation, ns.Length);

                var firstParen = this.code.IndexOf("{", nsLocation);
                this.code = this.code.Remove(firstParen, 1);

                var lastParen = this.code.LastIndexOf("}");
                this.code = this.code.Remove(lastParen, 1);
            }

            try
            {
                this.sensor = CSScript.RoslynEvaluator
                    .ReferenceAssembliesFromCode(code)
                    .ReferenceAssembly(Assembly.GetExecutingAssembly())
                    .ReferenceAssembly(Assembly.GetExecutingAssembly().Location)
                    .LoadCode<ISensor>(code);

                if (sensor.IsCompatibleWithCurrentRuntime() && sensor.DidSensorCompile())
                    IsCompiled = true;

                if (IsCompiled)
                    SensorIdentifier = sensor.GetSensorIdentifier().ToLower();
            }
            catch (Exception ex) { GetLastError = ex.Message; return; }
        }
    }
}