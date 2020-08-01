using PC2MQTT.Helpers;
using PC2MQTT.MQTT;
using PC2MQTT.Sensors;
using BadLogger;
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

        public static TimeSpan GetUpTime()
        {
            return TimeSpan.FromMilliseconds(GetTickCount64());
        }

        public bool DidSensorCompile() => true;

        public void Dispose()
        {
            Log.Debug($"Disposing [{GetSensorIdentifier()}]");
            Uninitialize();
            GC.SuppressFinalize(this);
        }

        public string GetSensorIdentifier() => this.GetType().Name;

        public bool Initialize(SensorHost sensorHost)
        {
            Log = LogManager.GetCurrentClassLogger();

            this.sensorHost = sensorHost;

            Log.Debug($"CPU id: {System.Threading.Thread.GetCurrentProcessorId()} ThreadId: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            IsInitialized = true;
            return true;
        }

        public void ProcessMessage(string topic, string message)
        {
            Log.Debug($"[{GetSensorIdentifier()}] Processing topic [{topic}]");

            if (topic == "/uptime/get")
            {
                Log.Info("Received uptime request");
                this.sensorHost.Publish("/uptime/current", GetUpTime().TotalMilliseconds.ToString(), prependDeviceId: true, retain: false);
            }
            else if (topic == "/uptime/current")
            {
                Log.Info("Received uptime message: " + message + "ms");
            }
        }

        public void Uninitialize()
        {
            fiveSeconds.Stop();
            fiveSeconds.Dispose();
        }

        [DllImport("kernel32")]
        private static extern UInt64 GetTickCount64();

        public void SensorMain()
        {
            Log.Debug($"(SensorMain) CPU id: {System.Threading.Thread.GetCurrentProcessorId()} ThreadId: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            this.sensorHost.Subscribe("/uptime/get");
            this.sensorHost.Subscribe("/uptime/current");

            Log.Info("Requesting my own uptime every 15 seconds..");

            fiveSeconds = new Timer(15000);
            fiveSeconds.Elapsed += delegate { this.sensorHost.Publish("/uptime/get", "", prependDeviceId: true, retain: false); };
            fiveSeconds.Start();
        }
    }
}