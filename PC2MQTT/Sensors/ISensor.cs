using PC2MQTT.MQTT;
using System;

namespace PC2MQTT.Sensors
{
    public interface ISensor : IDisposable
    {
        public bool IsInitialized { get; set; }

        public bool IsCompatibleWithCurrentRuntime();

        public SensorHost sensorHost { get; set; }

        public bool DidSensorCompile();

        public string GetSensorIdentifier();

        public bool Initialize(SensorHost sensorInfo);

        public void ProcessMessage(MqttMessage mqttMessage);

        public void SensorMain();

        public void Uninitialize();
    }
}