using System;
using System.Collections.Generic;
using System.Text;

namespace PC2MQTT.MQTT
{
    public interface IClient
    {
        public bool IsConnected { get; }

        public event MqttConnectionClosed ConnectionClosed;

        public event MqttConnectionConnected ConnectionConnected;

        public event MqttReconnecting ConnectionReconnecting;

        public event MqttMessagePublished MessagePublished;

        public event MessageReceivedByte MessageReceivedByte;

        public event MessageReceivedString MessageReceivedString;

        public event MqttTopicSubscribed TopicSubscribed;

        public event MqttTopicUnsubscribed TopicUnsubscribed;

        public void MqttConnect();

        public ushort Publish(string topic, string message, bool prependDeviceId = true, bool retain = false);
        public ushort Subscribe(string topic, bool prependDeviceId = true);
        public ushort Subscribe(string[] topics, bool prependDeviceId = true);
        public ushort Unubscribe(string topic, bool prependDeviceId = true);

    }
}
