using ExtensionMethods;
using System.Collections.Generic;
using System.Diagnostics;

namespace PC2MQTT.MQTT
{
    public class MqttMessage
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        /// <summary>
        /// This is a shorter version of AddDeviceIdToTopic()
        /// </summary>
        public MqttMessage AddDeviceId
        {
            get
            {
                this.AddDeviceIdToTopic();

                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessage AddMultiLevelWildcard
        {
            get
            {
                this.AddTopic("#");

                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessage AddSingleLevelWildcard
        {
            get
            {
                this.AddTopic("+");

                return this;
            }
        }

        public enum MqttMessageType
        {
            MQTT_NONE = 0,
            MQTT_PUBLISH,
            MQTT_SUBSCRIBE,
            MQTT_UNSUBSCRIBE,
            MQTT_RECEIVED
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessage PublishMessage
        {
            get
            {
                this.messageType = MqttMessageType.MQTT_PUBLISH;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessage QueueMessage
        {
            get
            {
                this.sendImmediately = false;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessage ReceivedMessage
        {
            get
            {
                this.messageType = MqttMessageType.MQTT_RECEIVED;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessage SendImmediately
        {
            get
            {
                this.sendImmediately = true;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessage SubscribeMessage
        {
            get
            {
                this.messageType = MqttMessageType.MQTT_SUBSCRIBE;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessage UnsubscribeMessage
        {
            get
            {
                this.messageType = MqttMessageType.MQTT_UNSUBSCRIBE;
                return this;
            }
        }

        public string deviceId = ExtensionMethods.Extensions.deviceId;
        public string message = "";
        public ushort messageId = 0;
        public MqttMessageType messageType = MqttMessageType.MQTT_NONE;
        public bool retain = false;
        public bool sendImmediately = false;
        private string _rawTopicCache = "";
        private List<string> topicList;

        public MqttMessage()
        {
            topicList = new List<string>();
        }

        public MqttMessage AddDeviceIdToTopic()
        {
            this.AddTopic(deviceId);

            return this;
        }

        public MqttMessage AddTopic(string topic)
        {
            if (topic.Length == 0)
                return this;

            if (topic.Contains("/"))
            {
                var topics = topic.Split("/");
                foreach (var item in topics)
                {
                    if (item.Length > 0)
                        topicList.Add(item);
                }
            }
            else
            {
                topicList.Add(topic);
            }

            UpdateRawTopicCache();

            return this;
        }

        public string GetDeviceId()
        {
            return deviceId;
        }

        public string GetMessage()
        {
            return message;
        }

        public ushort GetMessageId()
        {
            return messageId;
        }

        public string GetRawTopic()
        {
            return _rawTopicCache;
        }

        public string GetTopicWithoutDeviceId()
        {
            return GetRawTopic().RemoveDeviceId();
        }

        public MqttMessage SetMessage(string message)
        {
            this.message = message;

            return this;
        }

        public MqttMessage SetMessageId(ushort messageId)
        {
            this.messageId = messageId;

            return this;
        }

        public MqttMessage SetMessageType(MqttMessageType messageType)
        {
            this.messageType = messageType;

            return this;
        }

        public MqttMessage SetRetainFlag(bool retain)
        {
            this.retain = retain;

            return this;
        }

        /// <summary>
        /// Clears the current topic and replaces it.
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public MqttMessage SetTopic(string topic)
        {
            topicList.Clear();
            this.AddTopic(topic);

            return this;
        }

        private void UpdateRawTopicCache()
        {
            _rawTopicCache = string.Join("/", topicList);
        }
    }
}