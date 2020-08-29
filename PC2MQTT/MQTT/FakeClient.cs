﻿using BadLogger;
using ExtensionMethods;
using System;
using System.Text;
using System.Threading.Tasks;

namespace PC2MQTT.MQTT
{
    public class FakeClient : IClient
    {
        public event MqttConnectionClosed ConnectionClosed;

        public event MqttConnectionConnected ConnectionConnected;

        public event MqttReconnecting ConnectionReconnecting;

        public event MqttMessagePublished MessagePublished;

        //public event MessageReceivedByte MessageReceivedByte;

        public event MessageReceivedString MessageReceivedString;

        public event MqttTopicSubscribed TopicSubscribed;

        public event MqttTopicUnsubscribed TopicUnsubscribed;

        public bool IsConnected => true;
        private static BadLogger.BadLogger Log;
        private readonly Random _random = new Random();
        private ushort _messageId = 0;
        private MqttSettings _mqttSettings;

        public FakeClient(MqttSettings mqttSettings)
        {
            this._mqttSettings = mqttSettings;
            Log = LogManager.GetCurrentClassLogger();
            Log.Warn("***");
            Log.Warn("Initializing FakeClient. No actual MQTT connections will be made.");
            Log.Warn("Essentially a very basic, very fake MQTT internal server.");
            Log.Warn($"Using fake delays: {_mqttSettings.useFakeMqttDelays} | Using fake failures: {_mqttSettings.useFakeMqttFailures}");
            Log.Warn("***");
            System.Threading.Thread.Sleep(GetRandom());
        }

        public int GetRandom(int max = 2000)
        {
            if (!_mqttSettings.useFakeMqttDelays)
                return 0;

            return _random.Next(5, max);
        }

        public void MqttConnect()
        {
            System.Threading.Thread.Sleep(GetRandom(3000));

            ConnectionConnected?.Invoke();

            _messageId++;
        }

        public void MqttDisconnect()
        {
            System.Threading.Thread.Sleep(GetRandom());
            ConnectionClosed?.Invoke("Disconnected by MqttDisconnect", 99);
        }

        public bool SometimesFalse()
        {
            if (!_mqttSettings.useFakeMqttFailures)
                return true;

            return _random.Next(0, 9) != 0;
        }


        public void QueueMessage(MqttMessage message)
        {
            throw new NotImplementedException();
        }

        public MqttMessage Publish(MqttMessage message)
        {
            var success = SometimesFalse();

            if (!success) Log.Trace($"Randomly failing Publish call for {message.topic}: {message.message}");

            if (success)
            {
                message.messageId = _messageId++;

                MessagePublished?.Invoke(message);

                message.messageId = _messageId++;

                Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(GetRandom());
                    MessageReceivedString?.Invoke(message);
                });
            }
            return message;
        }

        public MqttMessage Subscribe(MqttMessage message)
        {

            System.Threading.Thread.Sleep(GetRandom(250));

            var success = SometimesFalse();
            if (!success) Log.Trace($"Randomly failing Subscribe call for {message.topic}");

            if (success)
            {
                message.messageId = _messageId++;
                TopicSubscribed?.Invoke(message);
            }

            return message;
        }

        public MqttMessage Unsubscribe(MqttMessage message)
        {
            System.Threading.Thread.Sleep(GetRandom(250));

            var success = SometimesFalse();
            if (!success) Log.Trace($"Randomly failing Unubscribe call for {message.topic}");

            if (success)
            {
                message.messageId = _messageId++;
                TopicUnsubscribed?.Invoke(message);
            }


            return message;
        }
    }
}