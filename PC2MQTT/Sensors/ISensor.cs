using System;

namespace PC2MQTT.Sensors
{
    public interface ISensor : IDisposable
    {
        public bool IsInitialized { get; set; }
        public SensorHost sensorHost { get; set; }

        public bool DidSensorCompile();

        public void SensorMain();

        public string GetSensorIdentifier();

        public bool Initialize(SensorHost sensorInfo);

        public void ProcessMessage(string topic, string message);

        public void Uninitialize();
    }
}