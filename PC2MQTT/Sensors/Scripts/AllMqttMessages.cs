using BadLogger;
using PC2MQTT.MQTT;
using System;
using System.Collections.Concurrent;

namespace PC2MQTT.Sensors
{
    public class AllMqttMessages : PC2MQTT.Sensors.ISensor
    {
        public bool IsInitialized { get; set; }

        public SensorHost sensorHost { get; set; }

        private ConcurrentDictionary<string, string> _cars = new ConcurrentDictionary<string, string>();
        private BadLogger.BadLogger Log;

        public bool DidSensorCompile() => true;

        public void Dispose()
        {
            if (IsInitialized)
                Log.Debug($"Disposing [{GetSensorIdentifier()}]");
            GC.SuppressFinalize(this);
        }

        public string GetSensorIdentifier() => this.GetType().Name;

        public bool Initialize(SensorHost sensorHost)
        {
            Log = LogManager.GetCurrentClassLogger(GetSensorIdentifier());

            this.sensorHost = sensorHost;

            Log.Info($"Finishing initialization in {this.GetSensorIdentifier()}");

            return true;
        }

        public bool IsCompatibleWithCurrentRuntime()
        {
            bool compatible = true;

            if (CSScriptLib.Runtime.IsCore) compatible = true;
            if (CSScriptLib.Runtime.IsLinux) compatible = true;
            if (CSScriptLib.Runtime.IsMono) compatible = true;
            if (CSScriptLib.Runtime.IsNet) compatible = true;
            if (CSScriptLib.Runtime.IsWin) compatible = true;

            return compatible;
        }

        public void ProcessMessage(MqttMessage mqttMessage)
        {
            Log.Info($"{mqttMessage.GetRawTopic()}: {mqttMessage.message}");
        }

        public void SensorMain()
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

        public void ServerStateChange(ServerState state, ServerStateReason reason)
        {
            Log.Debug($"ServerStateChange: {state}: {reason}");
        }

        public void Uninitialize()
        {
            if (IsInitialized)
            {
                Log.Info($"Uninitializing [{GetSensorIdentifier()}]");
            }
        }

    }
}