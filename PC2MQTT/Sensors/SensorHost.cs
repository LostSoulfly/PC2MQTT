using CSScriptLib;
using PC2MQTT.Helpers;
using PC2MQTT.MQTT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PC2MQTT.Sensors
{
    public class SensorHost
    {
        public ISensor sensor { get; private set; }
        public string topic { get; private set; }
        public string code { get; private set; }
        public bool IsCompiled { get; private set; }
        public bool IsCodeLoaded { get; private set; }
        public string SensorIdentifier { get; private set; }

        public string GetLastError { get; private set; }

        private Client _client;
        private string _deviceId;

        public SensorHost(string code, string deviceId, Client client)
        {
            this.code = code;
            this._deviceId = deviceId;
            this._client = client;

            IsCodeLoaded = true;

            Compile();
        }

        public SensorHost(string deviceId, Client client)
        {
            this._deviceId = deviceId;
            this._client = client;

            IsCodeLoaded = false;
            IsCompiled = false;
        }

        public void LoadFromFile(string filePath)
        {
            try
            {
                this.code = File.ReadAllText(filePath);
            } catch (Exception ex) { GetLastError = ex.Message; }

            IsCodeLoaded = true;
            Compile();
        }

        private void Compile()
        {
            if (!IsCodeLoaded)
                return;

            if (!this.code.Contains("using PC2MQTT.MQTT;"))
                this.code = "using PC2MQTT.MQTT;\r\n" + this.code;

            string ns = "namespace PC2MQTT.Sensors";

            if (this.code.Contains(ns))
            {
                Logging.Log("Sensor script contains a namespace. Attempting to remove it..");
                this.code = this.code.Remove(this.code.IndexOf(ns), ns.Length);

                var FirstParenLoc = this.code.IndexOf("{");
                this.code = this.code.Remove(FirstParenLoc, 1);

                var lastParenLoc = this.code.LastIndexOf("}");
                this.code = this.code.Substring(0, lastParenLoc > -1 ? lastParenLoc : this.code.Length);
            }

            try 
            {
                this.sensor = CSScript.Evaluator.LoadCode<ISensor>(code);

                IsCompiled = sensor.IsSensorReady();

                if (IsCompiled)
                {
                    topic = sensor.GetTopic();
                    SensorIdentifier = sensor.GetSensorIdentifier();
                }
            } catch (Exception ex) { GetLastError = ex.Message; return; }

}

        public bool InitializeSensor()
        {
            if (IsCompiled)
                return sensor.Initialize(this);

            return false;
        }

        public void Subscribe(string topic)
        {
            Logging.Log($"[{SensorIdentifier}] subscribing to [{topic}]");
            _client.Subscribe(topic);
        }

        public void Publish(string message, string topic, bool retain)
        {
            Logging.Log($"[{SensorIdentifier}] publishing to [{topic}]: [{message}]");
            _client.Publish(message, topic, retain);
        }

    }
}
