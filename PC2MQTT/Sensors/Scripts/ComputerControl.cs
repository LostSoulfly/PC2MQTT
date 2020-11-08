using BadLogger;
using PC2MQTT.MQTT;
using System;
using System.Timers;
using static PC2MQTT.MQTT.MqttMessage;

namespace PC2MQTT.Sensors
{
    public class ComputerControl : PC2MQTT.Sensors.ISensor
    {
        public bool IsInitialized { get; set; }

        public SensorHost sensorHost { get; set; }

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


        public void ProcessMessage(MqttMessage mqttMessage)
        {
            Log.Info($"[ProcessMessage] Processing topic [{mqttMessage.GetRawTopic()}]: {mqttMessage.message}");

            var topic = mqttMessage.GetTopicWithoutDeviceId().Split('/');

            if (topic == null || topic.Length <= 1)
                return;

            if (topic[0] == "computer")
                HandleCommand(topic[1..].ToString(), mqttMessage.message);
            else

                Log.Info($"[ProcessMessage] Unknown topic [{mqttMessage.GetRawTopic()}]");
        }

        private void HandleCommand(string command, string message)
        {

        }

        public void SensorMain()
        {
            Log.Info($"(SensorMain) CPU id: {System.Threading.Thread.GetCurrentProcessorId()} ThreadId: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            sensorHost.Publish(MqttMessageBuilder.
                NewMessage().
                AddDeviceIdToTopic.
                SubscribeMessage.
                AddTopic("computer").
                AddMultiLevelWildcard.
                DoNotRetain.
                QueueMessage.
                Build());

            while (this.IsInitialized)
            {
                //Log.Info("If you want, you can stay in control for the life of the sensor using something like this.");
                System.Threading.Thread.Sleep(10000);
            }
        }


        public void Uninitialize()
        {

            if (IsInitialized)
            {
                Log.Info($"Uninitializing [{GetSensorIdentifier()}]");

            }
        }

        public bool IsCompatibleWithCurrentRuntime()
        {

            bool compatible = true;


            if (CSScriptLib.Runtime.IsCore) compatible = true;
            if (CSScriptLib.Runtime.IsLinux) compatible = true;
            if (CSScriptLib.Runtime.IsMono) compatible = false;
            if (CSScriptLib.Runtime.IsNet) compatible = true;
            if (CSScriptLib.Runtime.IsWin) compatible = true;

            return compatible;
        }

        public void ServerStateChange(ServerState state, ServerStateReason reason)
        {
            Log.Debug($"ServerStateChange: {state}: {reason}");
        }
    }
}