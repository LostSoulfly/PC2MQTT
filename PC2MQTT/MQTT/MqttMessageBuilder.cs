using System;
using System.Collections.Generic;
using System.Text;
using static PC2MQTT.MQTT.MqttMessage;

namespace PC2MQTT.MQTT
{
    public class MqttMessageBuilder
    {
        private MqttMessage _mqttMessage;

        public MqttMessageBuilder()
        {
            _mqttMessage = new MqttMessage();
        }

        public MqttMessageBuilder SubscribeMessage { get
            {
                _mqttMessage.messageType = MqttMessageType.MQTT_SUBSCRIBE;
                return this;
            }
        }

        public MqttMessageBuilder PublishMessage
        {
            get
            {
                _mqttMessage.messageType = MqttMessageType.MQTT_PUBLISH;
                return this;
            }
        }

        public MqttMessageBuilder UnsubscribeMessage
        {
            get
            {
                _mqttMessage.messageType = MqttMessageType.MQTT_UNSUBSCRIBE;
                return this;
            }
        }
        public MqttMessageBuilder ReceivedMessage
        {
            get
            {
                _mqttMessage.messageType = MqttMessageType.MQTT_RECEIVED;
                return this;
            }
        }

        public MqttMessageBuilder Retain
        {
            get
            {
                _mqttMessage.retain = true;
                return this;
            }
        }

        public MqttMessageBuilder DoNotRetain
        {
            get
            {
                _mqttMessage.retain = false;
                return this;
            }
        }

        public MqttMessageBuilder AddDeviceIdToTopic
        {
            get
            {
                _mqttMessage.AddDeviceIdToTopic();
                return this;
            }
        }

        /// <summary>
        /// Shorthand version of AddDeviceIdToTopid.
        /// </summary>
        public MqttMessageBuilder AddDeviceId
        {
            get
            {
                _mqttMessage.AddDeviceIdToTopic();
                return this;
            }
        }

        public MqttMessageBuilder AddSingleLevelWildcard
        {
            get
            {
                _mqttMessage.AddTopic("+");
                return this;
            }
        }

        public MqttMessageBuilder AddMultiLevelWildcard()
        {
            _mqttMessage.AddTopic("#");
            return this;
        }

        public static MqttMessageBuilder NewMessage()
        {
            return new MqttMessageBuilder();
        }

        public MqttMessage Build()
        {
            return _mqttMessage;
        }

        public MqttMessageBuilder AddTopic(string topic)
        {

            _mqttMessage.AddTopic(topic);
            return this;
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
