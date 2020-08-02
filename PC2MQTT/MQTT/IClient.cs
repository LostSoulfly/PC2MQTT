namespace PC2MQTT.MQTT
{
    public interface IClient
    {
        public event MqttConnectionClosed ConnectionClosed;

        public event MqttConnectionConnected ConnectionConnected;

        public event MqttReconnecting ConnectionReconnecting;

        public event MqttMessagePublished MessagePublished;

        public event MessageReceivedByte MessageReceivedByte;

        public event MessageReceivedString MessageReceivedString;

        public event MqttTopicSubscribed TopicSubscribed;

        public event MqttTopicUnsubscribed TopicUnsubscribed;

        public bool IsConnected { get; }

        public void MqttConnect();

        public void MqttDisconnect();

        public ushort Publish(string topic, string message, bool prependDeviceId = true, bool retain = false);

        public ushort Subscribe(string topic, bool prependDeviceId = true);

        public ushort Unubscribe(string topic, bool prependDeviceId = true);
    }
}