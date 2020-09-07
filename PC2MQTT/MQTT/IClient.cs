namespace PC2MQTT.MQTT
{
    public interface IClient
    {
        public event MqttConnectionClosed ConnectionClosed;

        public event MqttConnectionConnected ConnectionConnected;

        public event MqttReconnecting ConnectionReconnecting;

        public event MqttMessagePublished MessagePublished;

        //public event MessageReceivedByte MessageReceivedByte;

        public event MessageReceivedString MessageReceivedString;

        public event MqttTopicSubscribed TopicSubscribed;

        public event MqttTopicUnsubscribed TopicUnsubscribed;

        public bool IsConnected { get; }

        public void MqttConnect();

        public void MqttDisconnect();

        public bool QueueMessage(MqttMessage message);

        public MqttMessage SendMessage(MqttMessage message);

        public MqttMessage Publish(MqttMessage message);

        public MqttMessage Subscribe(MqttMessage message);

        public MqttMessage Unsubscribe(MqttMessage message);
    }
}