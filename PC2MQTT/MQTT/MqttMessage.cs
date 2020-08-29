using PC2MQTT.Sensors;
using System;
using System.Collections.Generic;
using System.Text;

namespace PC2MQTT.MQTT
{
    public class MqttMessage
    {
        public string topic;
        public string message;
        public bool prependDeviceId = true;
        public bool retain = false;
        public MessageType messageType = MessageType.MQTT_PUBLISH;
        public ushort messageId;
        public ISensor sensor;

        public MqttMessage()
        {

        }


        public MqttMessage(ISensor sensor)
        {
            this.sensor = sensor;
        }

        public MqttMessage(string topic, string message, bool prependDeviceId = true, bool retain = false)
        {
            this.topic = topic;
            this.message = message;
            this.prependDeviceId = prependDeviceId;
            this.retain = retain;
        }

        public enum MessageType
        {
            MQTT_NONE = 0,
            MQTT_PUBLISH,
            MQTT_SUBSCRIBE,
            MQTT_UNSUBSCRIBE
        }
    }
}
