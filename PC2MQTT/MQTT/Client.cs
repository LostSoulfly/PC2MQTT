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

        private ConcurrentQueue<MqttMessage> _messageQueue = new ConcurrentQueue<MqttMessage>();

        public bool IsConnected => client.IsConnected;

        private bool _autoReconnect;

        private MqttSettings _mqttSettings;

        private System.Timers.Timer _reconnectTimer;

        private bool _reconnectTimerStarted;

        private AutoResetEvent _queueAutoResetEvent;

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

        public void MqttConnect()
        {
            if ((_autoReconnect) && (!_reconnectTimerStarted))
            {
                _reconnectTimer.Start();
                _reconnectTimerStarted = true;
            }

            if (client.IsConnected)
                client.Disconnect();

            System.Threading.Thread.Sleep(50);

            byte conn = client.Connect(_mqttSettings.deviceId, _mqttSettings.user, _mqttSettings.password, true, _mqttSettings.will.qosLevel,
                _mqttSettings.will.enabled, _mqttSettings.will.topic.ResultantTopic(true), _mqttSettings.will.offlineMessage, false, _mqttSettings.will.keepAlive);

            if (client.IsConnected)
            {
                var msg = new MqttMessage();
                msg.topic = _mqttSettings.will.topic;
                msg.message = _mqttSettings.will.onlineMessage;
                msg.messageType = MqttMessage.MessageType.MQTT_PUBLISH;

                Publish(msg);
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



            Task t;
            t = Task.Run(() => ProcessMessageQueue(), _queueCancellationTokenSource.Token);

        }

        private void ProcessMessageQueue()
        {
            while (!_queueCancellationTokenSource.Token.IsCancellationRequested)
            {

                bool dequeue = _messageQueue.TryPeek(out var msg);

                if (dequeue)
                {
                    switch (msg.messageType)
                    {
                        case MqttMessage.MessageType.MQTT_PUBLISH:
                            if (this.Publish(msg).messageId > 0)
                            {
                                _ = _messageQueue.TryDequeue(out var success);
                                if (success.messageId > 0) MessagePublished?.Invoke(success);
                            }
                            break;

                        case MqttMessage.MessageType.MQTT_SUBSCRIBE:
                            if (this.Subscribe(msg).messageId > 0)
                            {
                                _ = _messageQueue.TryDequeue(out var success);

                                if (success.messageId > 0) TopicSubscribed?.Invoke(success);
                            }
                            break;


                        case MqttMessage.MessageType.MQTT_UNSUBSCRIBE:
                            if (this.Unsubscribe(msg).messageId > 0)
                            {
                                _ = _messageQueue.TryDequeue(out var success);

                                if (success.messageId > 0) TopicUnsubscribed?.Invoke(success);
                            }
                            break;
                    }

                }

                _queueAutoResetEvent.WaitOne();
            }
        }

        public void MqttDisconnect()
        {
            client.Disconnect();
            ConnectionClosed?.Invoke("Disconnected by MqttDisconnect", 99);
            client = null;
        }

        public MqttMessage Publish(MqttMessage message)
        {
            // todo: Check for connectivity? Or QoS level if message will be retained
            message.messageId = client.Publish(message.topic.ResultantTopic(message.prependDeviceId),
                Encoding.UTF8.GetBytes(message.message),
                _mqttSettings.publishQosLevel,
                message.retain);

            if (message.messageId > 0) MessagePublished?.Invoke(message);

            return message;
        }

        public MqttMessage Subscribe(MqttMessage message)
        {
            message.messageId = client.Subscribe(new string[] { message.topic.ResultantTopic(message.prependDeviceId) }, new byte[] { _mqttSettings.subscribeQosLevel });

            if (message.messageId > 0) TopicSubscribed?.Invoke(message);

            return message;
        }

        public MqttMessage Unsubscribe(MqttMessage message)
        {
            message.messageId = client.Unsubscribe(new string[] { message.topic.ResultantTopic(message.prependDeviceId) });

            if (message.messageId > 0) TopicUnsubscribed?.Invoke(message);

            return message;
        }

        private void Client_ConnectionClosed(object sender, EventArgs e)
        {
            ConnectionClosed?.Invoke("Connection closed", 99);
        }

        /*
        private void Client_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            MessageReceivedByte?.Invoke(e.Topic.ResultantTopic(false), e.Message);
            MessageReceivedString?.Invoke(e.Topic.ResultantTopic(false), Encoding.UTF8.GetString(e.Message));
        }
        */

        public void QueueMessage(MqttMessage message)
        {
            _messageQueue.Enqueue(message);
        }
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