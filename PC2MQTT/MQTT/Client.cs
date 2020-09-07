using ExtensionMethods;
using PC2MQTT.Helpers;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using static PC2MQTT.MQTT.MqttMessage;

namespace PC2MQTT.MQTT
{
    //public delegate void MessageReceivedByte(string topic, byte[] message);

    public delegate void MessageReceivedString(MqttMessage mqttMessage);

    public delegate void MqttConnectionClosed(string reason, byte errorCode);

    public delegate void MqttConnectionConnected();

    public delegate void MqttMessagePublished(MqttMessage mqttMessage);

    public delegate void MqttReconnecting();

    public delegate void MqttTopicSubscribed(MqttMessage mqttMessage);

    public delegate void MqttTopicUnsubscribed(MqttMessage mqttMessage);

    public class Client : IClient
    {
        public event MqttConnectionClosed ConnectionClosed;

        public event MqttConnectionConnected ConnectionConnected;

        public event MqttReconnecting ConnectionReconnecting;

        public event MqttMessagePublished MessagePublished;

        //public event MessageReceivedByte MessageReceivedByte;

        public event MessageReceivedString MessageReceivedString;

        public event MqttTopicSubscribed TopicSubscribed;

        public event MqttTopicUnsubscribed TopicUnsubscribed;

        //private ConcurrentQueue<MqttMessage> _messageQueue = new ConcurrentQueue<MqttMessage>();

        //private BlockingCollection<MqttMessage> _messageQueue = new BlockingCollection<MqttMessage>();

        private BlockingCollection<MqttMessage> _messageQueue = new BlockingCollection<MqttMessage>(new ConcurrentBag<MqttMessage>(), 500);

        public bool IsConnected => client.IsConnected;

        private bool _autoReconnect;

        private MqttSettings _mqttSettings;

        private System.Timers.Timer _reconnectTimer;

        private bool _reconnectTimerStarted;

        private CancellationTokenSource _queueCancellationTokenSource;

        private MqttClient client;

        public Client(MqttSettings mqttSettings, bool autoReconnect = true)
        {
            this._mqttSettings = mqttSettings;
            this._autoReconnect = autoReconnect;

            client = new MqttClient(_mqttSettings.broker, _mqttSettings.port, false, null, null, MqttSslProtocols.None);

            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            client.ConnectionClosed += Client_ConnectionClosed;

            string clientId = _mqttSettings.deviceId;

            if (autoReconnect)
            {
                _reconnectTimer = new System.Timers.Timer(_mqttSettings.reconnectInterval);
                _reconnectTimer.Elapsed += delegate
                {
                    if (!client.IsConnected)
                    {
                        ConnectionReconnecting?.Invoke();
                        MqttConnect();
                    }
                };
            }
        }

        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            var msg = MqttMessageBuilder
                .NewMessage()
                .AddTopic(e.Topic)
                .SetMessage(Encoding.UTF8.GetString(e.Message))
                .SetMessageType(MqttMessageType.MQTT_PUBLISH)
                .Build();

            MessageReceivedString?.Invoke(msg);
        }

        public void MqttConnect()
        {
            if ((_autoReconnect) && (!_reconnectTimerStarted))
            {
                _reconnectTimer.Start();
                _reconnectTimerStarted = true;
            }

            if (client.IsConnected)
                client.Disconnect();

            System.Threading.Thread.Sleep(10);

            var offlineWill = MqttMessageBuilder
                .NewMessage()
                .AddDeviceIdToTopic
                .AddTopic(_mqttSettings.will.topic)
                .Build();

            byte conn = client.Connect(_mqttSettings.deviceId, _mqttSettings.user, _mqttSettings.password, true, _mqttSettings.will.qosLevel,
                _mqttSettings.will.enabled, offlineWill.GetRawTopic(), _mqttSettings.will.offlineMessage, false, _mqttSettings.will.keepAlive);

            if (client.IsConnected)
            {

                var onlineWill = MqttMessageBuilder
                    .NewMessage()
                    .AddDeviceIdToTopic
                    .AddTopic(_mqttSettings.will.topic)
                    .SetMessage(_mqttSettings.will.onlineMessage)
                    .SetMessageType(MqttMessageType.MQTT_PUBLISH)
                    .Build();

                Publish(onlineWill);
                ConnectionConnected?.Invoke();
            }
            else
            {
                switch (conn)
                {
                    case MqttMsgConnack.CONN_REFUSED_IDENT_REJECTED:
                        ConnectionClosed?.Invoke("Ident rejected by server", conn);
                        break;

                    case MqttMsgConnack.CONN_REFUSED_NOT_AUTHORIZED:
                        ConnectionClosed?.Invoke("User not authorized", conn);
                        break;

                    case MqttMsgConnack.CONN_REFUSED_PROT_VERS:
                        ConnectionClosed?.Invoke("Protocol version mismatch", conn);
                        break;

                    case MqttMsgConnack.CONN_REFUSED_SERVER_UNAVAILABLE:
                        ConnectionClosed?.Invoke("Server unavailable", conn);
                        break;

                    case MqttMsgConnack.CONN_REFUSED_USERNAME_PASSWORD:
                        ConnectionClosed?.Invoke("User/Pass error", conn);
                        break;

                    default:
                        ConnectionClosed?.Invoke("Unknown error", conn);
                        break;
                }
            }

            _queueCancellationTokenSource = new CancellationTokenSource();

            Task t;
            t = Task.Run(() => ProcessMessageQueue(), _queueCancellationTokenSource.Token);

        }

        private void ProcessMessageQueue()
        {
            while (!_queueCancellationTokenSource.Token.IsCancellationRequested)
                if (_messageQueue.TryTake(out var msg, 100))
                    ProcessMessage(msg);
        }

        private MqttMessage ProcessMessage(MqttMessage mqttMessage)
        {
            switch (mqttMessage.messageType)
            {
                case MqttMessage.MqttMessageType.MQTT_PUBLISH:
                    if (this.Publish(mqttMessage).messageId > 0)
                    {
                        _ = _messageQueue.TryTake(out var success);
                        if (mqttMessage.messageId > 0) MessagePublished?.Invoke(mqttMessage);
                        return mqttMessage;
                    }
                    break;

                case MqttMessage.MqttMessageType.MQTT_SUBSCRIBE:
                    if (this.Subscribe(mqttMessage).messageId > 0)
                    {
                        _ = _messageQueue.TryTake(out var success);

                        if (mqttMessage.messageId > 0) TopicSubscribed?.Invoke(mqttMessage);
                        return mqttMessage;
                    }
                    break;


                case MqttMessage.MqttMessageType.MQTT_UNSUBSCRIBE:
                    if (this.Unsubscribe(mqttMessage).messageId > 0)
                    {
                        _ = _messageQueue.TryTake(out var success);

                        if (mqttMessage.messageId > 0) TopicUnsubscribed?.Invoke(mqttMessage);
                        return mqttMessage;
                    }
                    break;
            }

            return mqttMessage;
        }

        public void MqttDisconnect()
        {
            client.Disconnect();
            ConnectionClosed?.Invoke("Disconnected by MqttDisconnect", 98);
            client = null;
        }

        public MqttMessage Publish(MqttMessage mqttMessage)
        {
            // todo: Check for connectivity? Or QoS level if message will be retained
            mqttMessage.messageId = client.Publish(mqttMessage.GetRawTopic(),
                Encoding.UTF8.GetBytes(mqttMessage.message),
                _mqttSettings.publishQosLevel,
                mqttMessage.retain);

            if (mqttMessage.messageId > 0) MessagePublished?.Invoke(mqttMessage);

            return mqttMessage;
        }

        public MqttMessage Subscribe(MqttMessage mqttMessage)
        {
            mqttMessage.messageId = client.Subscribe(new string[] { mqttMessage.GetRawTopic() }, new byte[] { _mqttSettings.subscribeQosLevel });

            if (mqttMessage.messageId > 0) TopicSubscribed?.Invoke(mqttMessage);

            return mqttMessage;
        }

        public MqttMessage Unsubscribe(MqttMessage mqttMessage)
        {
            mqttMessage.messageId = client.Unsubscribe(new string[] { mqttMessage.GetRawTopic() });

            if (mqttMessage.messageId > 0) TopicUnsubscribed?.Invoke(mqttMessage);

            return mqttMessage;
        }

        private void Client_ConnectionClosed(object sender, EventArgs e) => ConnectionClosed?.Invoke("Connection closed", 99);

        /*
        private void Client_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            MessageReceivedByte?.Invoke(e.Topic.ResultantTopic(false), e.Message);
            MessageReceivedString?.Invoke(e.Topic.ResultantTopic(false), Encoding.UTF8.GetString(e.Message));
        }
        */

        public bool QueueMessage(MqttMessage message)
        {
            return _messageQueue.TryAdd(message, 1000);
        }

        public MqttMessage SendMessage(MqttMessage message) => ProcessMessage(message);
    }

    public class MqttSettings
    {
        public string broker = "";
        public string deviceId = "PC2MQTT";
        public string password = "";
        public int port = 1883;
        public byte publishQosLevel = 2;
        public int reconnectInterval = 10000;
        public byte subscribeQosLevel = 2;
        public bool useFakeMqttDelays = true;
        public bool useFakeMqttFailures = false;
        public bool useFakeMqttServer = false;
        public string user = "";
        public MqttWill will = new MqttWill();
    }

    public class MqttWill
    {
        public bool enabled = true; // WillFlag
        public ushort keepAlive = 60000;
        public string offlineMessage = "Offline";
        public string onlineMessage = "Online";
        public byte qosLevel = 2;
        public bool retain = true;
        public string topic = "/status";
    }
}