using PC2MQTT.MQTT;
using PC2MQTT.Sensors;

namespace PC2MQTT.Sensors
{
    public class test : PC2MQTT.Sensors.ISensor
    {
        public string GetSensorIdentifier() => "test";
        public bool IsInitialized { get; set; }
        public bool IsSensorReady() => true;

        SensorHost _sensorHost;

        public string GetTopic()
        {
            return "/test";
        }

        public bool Initialize(SensorHost sensorHost)
        {
            this._sensorHost = sensorHost;

            this._sensorHost.Subscribe(GetTopic());

            this._sensorHost.Publish("hello world", GetTopic(), false);


            IsInitialized = true;
            return true;
        }


        public void ProcessMessage(string message)
        {
            
        }
    }
}
