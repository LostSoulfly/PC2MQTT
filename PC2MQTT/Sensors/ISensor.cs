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

        public void ServerStateChange(ServerState state, ServerStateReason reason);
    }

    public enum ServerState
    {
        Connected = 0,
        Disconnected,
        Disconnecting,
        Reconnected,
        Reconnecting
    }

    public enum ServerStateReason
    {
        ShuttingDown = 0,
        ServerDisconnected,
        ClientDisconnected,
        Unknown
    }
}