using ExtensionMethods;
using PC2MQTT.Sensors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PC2MQTT.MQTT
{
    public class MqttMessage
    {
        private List<string> topicList;
        public bool retain = false;
        public MqttMessageType messageType = MqttMessageType.MQTT_PUBLISH;
        public ushort messageId = 0;
        public string message = "";
        public string deviceId = ExtensionMethods.Extensions.deviceId;
        private string _rawTopicCache = "";

        public MqttMessage()
        {
            topicList = new List<string>();
        }

        public MqttMessage SetMessage(string message)
        {
            this.message = message;

            return this;
        }

        public string GetMessage()
        {
            return message;
        }

        public MqttMessage SetRetainFlag(bool retain)
        {
            this.retain = retain;

            return this;
        }

        public MqttMessage SetMessageType(MqttMessageType messageType)
        {
            this.messageType = messageType;

            return this;
        }

        [DebuggerHidden]
        public MqttMessage SubscribeMessage
        {
            get
            {
                this.messageType = MqttMessageType.MQTT_SUBSCRIBE;
                return this;
            }
        }

        [DebuggerHidden]
        public MqttMessage PublishMessage
        {
            get
            {
                this.messageType = MqttMessageType.MQTT_PUBLISH;
                return this;
            }
        }

        [DebuggerHidden]
        public MqttMessage UnsubscribeMessage
        {
            get
            {
                this.messageType = MqttMessageType.MQTT_UNSUBSCRIBE;
                return this;
            }
        }

        [DebuggerHidden]
        public MqttMessage ReceivedMessage
        {
            get
            {
                this.messageType = MqttMessageType.MQTT_RECEIVED;
                return this;
            }
        }

        public MqttMessage SetMessageId(ushort messageId)
        {
            this.messageId = messageId;

            return this;
        }

        public ushort GetMessageId()
        {
            return messageId;
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

        public string GetRawTopic()
        {
            return _rawTopicCache;
        }

        private void UpdateRawTopicCache()
        {
            _rawTopicCache = string.Join("/", topicList);
        }

        public string GetTopicWithoutDeviceId()
        {
            return GetRawTopic().RemoveDeviceId();
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

        public MqttMessage AddDeviceIdToTopic()
        {
            this.AddTopic(deviceId);

            return this;
        }

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

        [DebuggerHidden]
        public MqttMessage AddSingleLevelWildcard
        {
            get
            {
                this.AddTopic("+");

                return this;
            }
        }

        [DebuggerHidden]
        public MqttMessage AddMultiLevelWildcard
        {
            get
            {
                this.AddTopic("#");

                return this;
            }
        }
        

        public string GetDeviceId()
        {
            return deviceId;
        }

        public enum MqttMessageType
        {
            MQTT_NONE = 0,
            MQTT_PUBLISH,
            MQTT_SUBSCRIBE,
            MQTT_UNSUBSCRIBE,
            MQTT_RECEIVED
        }
    }
}
