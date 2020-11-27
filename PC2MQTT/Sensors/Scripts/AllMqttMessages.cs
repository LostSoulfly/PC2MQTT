using BadLogger;
using PC2MQTT.MQTT;
using System;
using System.Collections.Concurrent;

namespace PC2MQTT.Sensors
{
    public partial class AllMqttMessages : SensorBase, PC2MQTT.Sensors.ISensor
    {

        public new void ProcessMessage(MqttMessage mqttMessage)
        {
            Log.Info($"{mqttMessage.GetRawTopic()}: {mqttMessage.message}");
        }

        public new void SensorMain()
        {
            base.SensorMain();

            // Subscribe to ALL mqtt messages with a root multi-level wildcard
            sensorHost.Subscribe(MqttMessageBuilder.
                NewMessage().
                SubscribeMessage.
                AddMultiLevelWildcard.
                DoNotRetain.
                QueueMessage.
                Build());
        }

    }
}