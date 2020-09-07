using System;
using System.Collections.Generic;
using System.Text;
using static PC2MQTT.MQTT.MqttMessage;

namespace PC2MQTT.MQTT
{
    public interface IMqttMessage2
    {
        public void SetMessage(string message);
        public string GetMessage();
        public void SetRetainFlag(bool retain);
        public void SetMessageType(MqttMessageType messageType);
        public void SetMessageId(ushort messageId);
        public ushort GetMessageId();
        public void SetTopic(string topic);
        public string GetTopic();
    }
}
