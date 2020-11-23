using BadLogger;
using PC2MQTT.MQTT;
using System;

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

        public void ProcessMessage(MqttMessage mqttMessage)
        {
            Log.Info($"[ProcessMessage] Processing topic [{mqttMessage.GetRawTopic()}]: {mqttMessage.message}");

            var topic = mqttMessage.GetTopicWithoutDeviceId().Split('/');

            if (topic == null || topic.Length <= 1)
                return;

            if (topic[0] == "computer")
                HandleCommand(topic, mqttMessage.message);
            else
                Log.Info($"[ProcessMessage] Unknown topic [{mqttMessage.GetRawTopic()}]");
        }

        public void SensorMain()
        {
            Log.Info($"(SensorMain) CPU id: {System.Threading.Thread.GetCurrentProcessorId()} ThreadId: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            sensorHost.Subscribe(MqttMessageBuilder.
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
                sensorHost.Publish(MqttMessageBuilder.
                NewMessage().
                AddDeviceIdToTopic.
                PublishMessage.
                AddTopic("computer").
                AddTopic("shutdown").
                DoNotRetain.
                QueueMessage.
                Build());
            }
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

        private void HandleCommand(string[] command, string message)
        {
            var args = message;
            Log.Info($"[HandleCommand] Processing [{String.Join("/", command[1..])}] with args [{message}]");

            switch (command[0])
            {
                case "shutdown":
                    break;

                case "restart":
                case "reboot":
                    break;

                case "lock":
                    break;

                case "run":
                case "shell":
                    break;

                case "sleep":
                    break;

                case "hibernate":
                    break;

                case "monitor":
                    break;

                case "audio":
                case "sound":
                    break;

                case "screenshot":
                    break;

                case "ping":
                    break;

                case "uptime":
                    break;

                default:
                    break;
            }
        }
    }
}