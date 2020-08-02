using BadLogger;
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

        public event MessageReceivedByte MessageReceivedByte;

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

        public ushort Publish(string topic, string message, bool prependDeviceId = true, bool retain = false)
        {
            var success = SometimesFalse();

            if (!success) Log.Trace($"Randomly failing Publish call for {topic}: {message}");

            if (success)
            {
                if (_messageId > 0) MessagePublished?.Invoke(topic, message);

                Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(GetRandom());
                    MessageReceivedByte?.Invoke(topic.ResultantTopic(false), Encoding.UTF8.GetBytes(message));
                    MessageReceivedString?.Invoke(topic.ResultantTopic(false), message);
                });
            }
            else { return 0; }
            return _messageId++;
        }

        public bool SometimesFalse()
        {
            if (!_mqttSettings.useFakeMqttFailures)
                return true;

            return _random.Next(0, 9) != 0;
        }

        public ushort Subscribe(string topic, bool prependDeviceId = true)
        {
            System.Threading.Thread.Sleep(GetRandom(250));

            var success = SometimesFalse();
            if (!success) Log.Trace($"Randomly failing Subscribe call for {topic}");

            if (success)
                TopicSubscribed?.Invoke(topic);
            else
                return 0;

            return _messageId++;
        }

        public ushort Unubscribe(string topic, bool prependDeviceId = true)
        {
            System.Threading.Thread.Sleep(GetRandom(250));

            var success = SometimesFalse();
            if (!success) Log.Trace($"Randomly failing Unubscribe call for {topic}");

            if (success)
                TopicUnsubscribed?.Invoke(topic);
            else
                return 0;

            return _messageId++;
        }
    }
}