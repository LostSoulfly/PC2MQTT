﻿using PC2MQTT.Helpers;
using System;
using System.Text;
using System.Timers;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace PC2MQTT.MQTT
{
    public delegate void MessageReceivedString(string message, string topic);

    public delegate void MessageReceivedByte(byte message, string topic);

    public delegate void MqttMessagePublished(ushort messageId, bool isPublished);

    public delegate void MqttTopicSubscribed(ushort messageId);

    public delegate void MqttTopicUnsubscribed(ushort messageId);

    public delegate void MqttConnectionClosed(string reason, byte errorCode);

    public delegate void MqttConnectionConnected();

    public delegate void MqttReconnecting();

    public class MqttSettings
    {
        public string broker = "";
        public int port = 1883;
        public string user = "";
        public string password = "";
        public string deviceId = "PC2MQTT";
        public byte publishQosLevel = 2;
        public byte subscribeQosLevel = 2;
        public int reconnectInterval = 10000;
        public MqttWill will = new MqttWill();
    }

    public class MqttWill
    {
        public bool enabled = true; // WillFlag
        public bool retain = true;
        public byte qosLevel = 2;
        public ushort keepAlive = 60000;
        public string topic = "/status";
        public string offlineMessage = "Offline";
        public string onlineMessage = "Online";
    }

    public class Client
    {
        private MqttClient client;
        private MqttSettings _mqttSettings;
        private bool _autoReconnect;
        private bool _reconnectTimerStarted;
        public bool IsConnected => client.IsConnected;

        public event MessageReceivedString MessageReceivedString;

        public event MessageReceivedByte MessageReceivedByte;

        public event MqttMessagePublished MessagePublished;

        public event MqttTopicSubscribed TopicSubscribed;

        public event MqttTopicUnsubscribed TopicUnsubscribed;

        public event MqttConnectionClosed ConnectionClosed;

        public event MqttConnectionConnected ConnectionConnected;

        public event MqttReconnecting ConnectionReconnecting;

        private Timer _reconnectTimer;

        public Client(MqttSettings mqttSettings, bool autoReconnect = true)
        {
            this._mqttSettings = mqttSettings;
            this._autoReconnect = autoReconnect;

            client = new MqttClient(_mqttSettings.broker, _mqttSettings.port, false, null, null, MqttSslProtocols.None);

            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            client.MqttMsgPublished += Client_MqttMsgPublished;
            client.MqttMsgSubscribed += Client_MqttMsgSubscribed;
            client.MqttMsgUnsubscribed += Client_MqttMsgUnsubscribed;
            client.ConnectionClosed += Client_ConnectionClosed;

            string clientId = _mqttSettings.deviceId;

            if (autoReconnect)
            {
                _reconnectTimer = new Timer(_mqttSettings.reconnectInterval);
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

        private void Client_ConnectionClosed(object sender, EventArgs e)
        {
            ConnectionClosed?.Invoke("Connection closed", 99);
        }

        public void MqttConnect()
        {

            if ((_autoReconnect) && (!_reconnectTimerStarted)) { 
                _reconnectTimer.Start();
                _reconnectTimerStarted = true;
            }


            if (client.IsConnected)
                client.Disconnect();

            System.Threading.Thread.Sleep(50);

            byte conn = client.Connect(_mqttSettings.deviceId, _mqttSettings.user, _mqttSettings.password, true, _mqttSettings.will.qosLevel,
                _mqttSettings.will.enabled, _mqttSettings.deviceId + _mqttSettings.will.topic, _mqttSettings.will.offlineMessage, false, _mqttSettings.will.keepAlive);

            if (client.IsConnected)
            {
                Publish(_mqttSettings.will.onlineMessage, _mqttSettings.will.topic);
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
        }

        public ushort Publish(string message, string topic, bool retain = false)
        {
            // Check for connectivity? Or QoS level if message will be retained
            var messageId = client.Publish(_mqttSettings.deviceId + topic, Encoding.UTF8.GetBytes(message), _mqttSettings.publishQosLevel, retain);

            return messageId;
        }

        public ushort Subscribe(string topic)
        {
            var messageId = client.Subscribe(new string[] { _mqttSettings.deviceId + topic }, new byte[] { _mqttSettings.subscribeQosLevel });

            return messageId;
        }

        public ushort Subscribe(string[] topics)
        {
            string topicsString = String.Join(", ", topics);

            ushort messageId = 0;

            foreach (var item in topics)
            {
                messageId = Subscribe(item);
            }

            //only return last messageId..
            return messageId;
        }

        private void Client_MqttMsgUnsubscribed(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgUnsubscribedEventArgs e)
        {
            TopicUnsubscribed?.Invoke(e.MessageId);
        }

        private void Client_MqttMsgSubscribed(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgSubscribedEventArgs e)
        {
            TopicSubscribed?.Invoke(e.MessageId);
        }

        private void Client_MqttMsgPublished(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishedEventArgs e)
        {
            MessagePublished?.Invoke(e.MessageId, e.IsPublished);
        }

        private void Client_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            MessageReceivedString?.Invoke(Encoding.UTF8.GetString(e.Message), e.Topic);
        }
    }
}