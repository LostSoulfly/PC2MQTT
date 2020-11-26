using BadLogger;
using PC2MQTT.MQTT;
using System;
using System.Collections.Concurrent;

namespace PC2MQTT.Sensors
{
    public partial class AllMqttMessages : SensorBase, PC2MQTT.Sensors.ISensor
    {
        private ConcurrentDictionary<string, string> _cars = new ConcurrentDictionary<string, string>();

        public new bool Initialize(SensorHost sensorHost)
        {
            Log = LogManager.GetCurrentClassLogger(GetSensorIdentifier());

            this.sensorHost = sensorHost;

            Log.Info($"Finishing initialization in {this.GetSensorIdentifier()}");

            return true;
        }

        public new bool IsCompatibleWithCurrentRuntime()
        {
            bool compatible = true;

            if (CSScriptLib.Runtime.IsCore) compatible = true;
            if (CSScriptLib.Runtime.IsLinux) compatible = true;
            if (CSScriptLib.Runtime.IsMono) compatible = true;
            if (CSScriptLib.Runtime.IsNet) compatible = true;
            if (CSScriptLib.Runtime.IsWin) compatible = true;

            return compatible;
        }

        public new void ProcessMessage(MqttMessage mqttMessage)
        {
            Log.Info($"{mqttMessage.GetRawTopic()}: {mqttMessage.message}");
        }

        public new void SensorMain()
        {
            Log.Info($"(SensorMain) CPU id: {System.Threading.Thread.GetCurrentProcessorId()} ThreadId: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            // Subscribe to ALL mqtt messages with a root multi-level wildcard
            sensorHost.Subscribe(MqttMessageBuilder.
                NewMessage().
                SubscribeMessage.
                AddMultiLevelWildcard.
                DoNotRetain.
                QueueMessage.
                Build());
        }

    }
}