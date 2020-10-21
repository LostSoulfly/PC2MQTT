using BadLogger;
using ExtensionMethods;
using PC2MQTT.MQTT;
using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace PC2MQTT.Sensors
{
    public class Uptime : PC2MQTT.Sensors.ISensor
    {
        public bool IsInitialized { get; set; }

        public SensorHost sensorHost { get; set; }

        private Timer fiveSeconds;

        private BadLogger.BadLogger Log;

        public bool DidSensorCompile() => true;

        public bool IsCompatibleWithCurrentRuntime() => true;

        public void Dispose()
        {
            Log.Debug($"Disposing [{GetSensorIdentifier()}]");
            Log = null;
            fiveSeconds.Dispose();
            sensorHost = null;
        }

        public string GetSensorIdentifier() => this.GetType().Name;

        public TimeSpan GetUpTime()
        {
            if (CSScriptLib.Runtime.IsLinux || CSScriptLib.Runtime.IsMono)
            {
                string output = "cat /proc/uptime".LinuxBashResult();
                var uptime = output.Split(" ");
                return TimeSpan.FromSeconds(double.Parse(uptime[0]));
            }

            if (CSScriptLib.Runtime.IsWin)
            {
                //string cmd = "wmic path Win32_OperatingSystem get LastBootUpTime".WindowsShellResult();
                //Log.Info("cmd: " + cmd);

                return TimeSpan.FromMilliseconds(GetTickCount64());
            }

            return TimeSpan.MinValue;
        }

        public bool Initialize(SensorHost sensorHost)
        {
            Log = LogManager.GetCurrentClassLogger(GetSensorIdentifier());
            this.sensorHost = sensorHost;

            return true;
        }

        public void ProcessMessage(MqttMessage mqttMessage)
        {
            Log.Debug($"[{GetSensorIdentifier()}] Processing topic [{mqttMessage.GetRawTopic()}]");

            if (mqttMessage.GetTopicWithoutDeviceId() == "uptime/get")
            {
                Log.Info("Received uptime request");
                if (!sensorHost.Publish(new MqttMessageBuilder().PublishMessage.AddDeviceIdToTopic.AddTopic("/uptime/current").SetMessage(GetUpTime().TotalMilliseconds.ToString()).DoNotRetain.Build()))
                    Log.Info("Failed to publish to /example1/status");
            }
            else if (mqttMessage.GetTopicWithoutDeviceId() == "uptime/current")
            {
                Log.Info("Received uptime message: " + mqttMessage.message + "ms");
            }
        }

        public void SensorMain()
        {
            Log.Debug($"(SensorMain) CPU id: {System.Threading.Thread.GetCurrentProcessorId()} ThreadId: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            //this.sensorHost.Subscribe("/uptime/get");
            //this.sensorHost.Subscribe("/uptime/current");


            var msg = MqttMessageBuilder.NewMessage().SubscribeMessage.AddDeviceIdToTopic.AddTopic("/uptime/").AddSingleLevelWildcard.DoNotRetain.Build();
            if (!sensorHost.Subscribe(msg))
                Log.Info("Failed to subscribe to /example2/#");

            Log.Info("Requesting my own uptime every 15 seconds..");

            fiveSeconds = new Timer(15000);
            fiveSeconds.Elapsed += delegate { sensorHost.Publish(new MqttMessageBuilder().PublishMessage.AddDeviceIdToTopic.AddTopic("/uptime/get").DoNotRetain.Build()); };

                fiveSeconds.Start();
        }

        public void Uninitialize()
        {
            if (fiveSeconds != null)
            {
                fiveSeconds.Stop();
                fiveSeconds.Dispose();
            }
        }

        [DllImport("kernel32")]
        private static extern UInt64 GetTickCount64();

    }
}