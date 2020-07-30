using System;
using System.Collections.Generic;
using System.Text;

namespace PC2MQTT.Sensors
{
    public interface ISensor
    {
        public bool IsInitialized { get; set; }
        public bool Initialize(SensorHost sensorInfo);
        public string GetTopic();
        public void ProcessMessage(string message);
        public bool IsSensorReady();
        public string GetSensorIdentifier();
    }
}
