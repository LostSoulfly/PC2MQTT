using BadLogger;
using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace PC2MQTT.MQTT
{
    public class FakeClient : IClient
    {

        private ushort _messageId = 0;
        public bool IsConnected => true;

        public event MqttConnectionClosed ConnectionClosed;
        public event MqttConnectionConnected ConnectionConnected;
        public event MqttReconnecting ConnectionReconnecting;
        public event MqttMessagePublished MessagePublished;
        public event MessageReceivedByte MessageReceivedByte;
        public event MessageReceivedString MessageReceivedString;
        public event MqttTopicSubscribed TopicSubscribed;
        public event MqttTopicUnsubscribed TopicUnsubscribed;


        private static BadLogger.BadLogger Log;
        private MqttSettings _mqttSettings;


        public FakeClient(MqttSettings mqttSettings)
        {

            this._mqttSettings = mqttSettings;
            Log = LogManager.GetCurrentClassLogger();
            Log.Warn("***");
            Log.Warn("Initializing FakeClient. No actual MQTT connections will be made.");
            Log.Warn("Essentially a very basic MQTT server hosted locally.");
            Log.Warn("***");

        }


        public void MqttConnect()
        {
            ConnectionConnected?.Invoke();
        }

        public ushort Publish(string topic, string message, bool prependDeviceId = true, bool retain = false)
        {
            if (_messageId > 0) MessagePublished?.Invoke(topic, message);

            MessageReceivedByte?.Invoke(topic.ResultantTopic(false), Encoding.UTF8.GetBytes(message));
            MessageReceivedString?.Invoke(topic.ResultantTopic(false), message);

            return _messageId++;
        }

        public ushort Subscribe(string topic, bool prependDeviceId = true)
        {
            if (_messageId > 0) TopicSubscribed?.Invoke(topic);
            return _messageId++;
        }

        public ushort Subscribe(string[] topics, bool prependDeviceId = true)
        {
            string topicsString = String.Join(", ", topics);

            ushort messageId = 0;

            foreach (var item in topics)
            {
                messageId = Subscribe(item, prependDeviceId);
            }
            return messageId;
        }

        public ushort Unubscribe(string topic, bool prependDeviceId = true)
        {
            if (_messageId > 0) TopicUnsubscribed?.Invoke(topic);
            return _messageId++;
        }
    }
}
