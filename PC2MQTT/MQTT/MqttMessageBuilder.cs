using System.Diagnostics;
using static PC2MQTT.MQTT.MqttMessage;

namespace PC2MQTT.MQTT
{
    public class MqttMessageBuilder
    {
        /// <summary>
        /// Shorthand version of AddDeviceIdToTopic.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder AddDeviceId
        {
            get
            {
                _mqttMessage.AddDeviceIdToTopic();
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder AddDeviceIdToTopic
        {
            get
            {
                _mqttMessage.AddDeviceIdToTopic();
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder AddMultiLevelWildcard
        {
            get
            {
                _mqttMessage.AddTopic("#");
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder AddSingleLevelWildcard
        {
            get
            {
                _mqttMessage.AddTopic("+");
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder DoNotRetain
        {
            get
            {
                _mqttMessage.retain = false;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder PublishMessage
        {
            get
            {
                _mqttMessage.messageType = MqttMessageType.MQTT_PUBLISH;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder QueueMessage
        {
            get
            {
                _mqttMessage.sendImmediately = false;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder ReceivedMessage
        {
            get
            {
                _mqttMessage.messageType = MqttMessageType.MQTT_RECEIVED;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder Retain
        {
            get
            {
                _mqttMessage.retain = true;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder SendImmediately
        {
            get
            {
                _mqttMessage.sendImmediately = true;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder SubscribeMessage
        {
            get
            {
                _mqttMessage.messageType = MqttMessageType.MQTT_SUBSCRIBE;
                return this;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public MqttMessageBuilder UnsubscribeMessage
        {
            get
            {
                _mqttMessage.messageType = MqttMessageType.MQTT_UNSUBSCRIBE;
                return this;
            }
        }

        private MqttMessage _mqttMessage;

        public MqttMessageBuilder()
        {
            _mqttMessage = new MqttMessage();
        }

        public static MqttMessageBuilder NewMessage()
        {
            return new MqttMessageBuilder();
        }

        public MqttMessageBuilder AddTopic(string topic)
        {
            _mqttMessage.AddTopic(topic);
            return this;
        }

        public MqttMessage Build()
        {
            return _mqttMessage;
        }

        public MqttMessageBuilder SetMessage(string message)
        {
            _mqttMessage.SetMessage(message);
            return this;
        }

        public MqttMessageBuilder SetMessageType(MqttMessageType messageType)
        {
            _mqttMessage.messageType = messageType;
            return this;
        }
    }
}