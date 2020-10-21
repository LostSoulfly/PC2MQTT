using BadLogger;
using ExtensionMethods;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
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

        private CancellationTokenSource _queueCancellationTokenSource;

        private BlockingCollection<MqttMessage> _messageQueue = new BlockingCollection<MqttMessage>(new ConcurrentQueue<MqttMessage>(), 500);

        public FakeClient(MqttSettings mqttSettings)
        {
            this._mqttSettings = mqttSettings;
            Log = LogManager.GetCurrentClassLogger("FakeMqtt");
            Log.Warn("***");
            Log.Warn("Initializing FakeClient. No actual MQTT connections will be made.");
            Log.Warn("Essentially a very basic, very fake MQTT internal server.");
            Log.Warn($"Using fake delays: {_mqttSettings.useFakeMqttDelays} | Using fake failures: {_mqttSettings.useFakeMqttFailures}");
            Log.Warn("***");
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
            _queueCancellationTokenSource = new CancellationTokenSource();

            Task t;
            t = Task.Run(() => ProcessMessageQueue(), _queueCancellationTokenSource.Token);

        }

        private void ProcessMessageQueue()
        {
            Log.Trace("Starting Message Queue Processing..");
            while (!_queueCancellationTokenSource.Token.IsCancellationRequested)
            {
                var msg = _messageQueue.Take();

                Log.Trace($"Process msg queue: [{msg.messageType}] {msg.GetRawTopic()}: {msg.message}");
                System.Threading.Thread.Sleep(GetRandom(500));
                ProcessMessage(msg);
            }
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


        public bool QueueMessage(MqttMessage message)
        {
            _messageQueue.Add(message);

            return true;
        }

        private MqttMessage ProcessMessage(MqttMessage msg)
        {
            switch (msg.messageType)
            {
                case MqttMessage.MqttMessageType.MQTT_PUBLISH:
                    this.Publish(msg);
                        //MessagePublished?.Invoke(msg);
                    break;

                case MqttMessage.MqttMessageType.MQTT_SUBSCRIBE:
                    this.Subscribe(msg);
                        //TopicSubscribed?.Invoke(msg);
                    break;

                case MqttMessage.MqttMessageType.MQTT_UNSUBSCRIBE:
                    this.Unsubscribe(msg);
                        //TopicUnsubscribed?.Invoke(msg);
                    break;
            }

            return msg;
        }


        public MqttMessage Publish(MqttMessage message)
        {
            var success = SometimesFalse();

            if (!success) Log.Trace($"Randomly failing Publish call for {message.GetRawTopic()}: {message.message}");

            if (success)
            {
                message.messageId = _messageId++;

                MessagePublished?.Invoke(message);

                message.messageId = _messageId++;

                /*
                Task.Run(() =>
                {
                    //System.Threading.Thread.Sleep(GetRandom());
                    //message.prependDeviceId = false;
                    MessageReceivedString?.Invoke(message);
                });
                */

                System.Threading.Thread.Sleep(GetRandom(100));
                MessageReceivedString?.Invoke(message);
            }
            return message;
        }

        public MqttMessage Subscribe(MqttMessage message)
        {

            //System.Threading.Thread.Sleep(GetRandom(250));

            var success = SometimesFalse();
            if (!success) Log.Trace($"Randomly failing Subscribe call for {message.GetRawTopic()}");

            if (success)
            {
                message.messageId = _messageId++;
                //message.prependDeviceId = false;
                TopicSubscribed?.Invoke(message);
            }

            return message;
        }

        public MqttMessage Unsubscribe(MqttMessage message)
        {
            //System.Threading.Thread.Sleep(GetRandom(250));

            var success = SometimesFalse();
            if (!success) Log.Trace($"Randomly failing Unubscribe call for {message.GetRawTopic()}");

            if (success)
            {
                message.messageId = _messageId++;
                //message.prependDeviceId = false;
                TopicUnsubscribed?.Invoke(message);
            }


            return message;
        }

        public MqttMessage SendMessage(MqttMessage message)
        {
            return ProcessMessage(message);
        }
    }
}