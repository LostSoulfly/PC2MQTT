using System;
using System.Collections.Generic;
using System.Text;

namespace PC2MQTT.Sensors
{
    public interface ISensor : IDisposable
    {
        public SensorHost sensorHost { get; set; }
        public bool IsInitialized { get; set; }
        public bool Initialize(SensorHost sensorInfo);
        public void Uninitialize();
        public void ProcessMessage(string topic, string message);
        public bool DidSensorCompile();
        public string GetSensorIdentifier();
    }
}
