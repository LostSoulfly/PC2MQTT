using BadLogger;
using PC2MQTT.MQTT;
using System;
using System.Timers;

// Namespaces aren't allowed, so either remove this (and its {} brackets) or leave it alone
// PC2MQTT attempts to remove namespaces automatically before compiling the sensor script
namespace PC2MQTT.Sensors
{
    // Change "Example" to whatever you want your Sensor to be. It should still inherit from ISensor!
    public class Example : PC2MQTT.Sensors.ISensor
    {
        // If we've finished initializing. Set this to true before returning from Initialize() below.
        public bool IsInitialized { get; set; }

        // Save this from the Initialize entrypoint, passed from the parent sensorHost
        public SensorHost sensorHost { get; set; }

        // BadLogger from PC2MQTT so we can pass log messages, not required
        private BadLogger.BadLogger Log;

        // An example timer, see more below in SensorMain()
        private Timer unloadTimer;

        // This should always return true. This is a simple test to see if the sensor was loaded properly
        public bool DidSensorCompile() => true;

        // You can call this directly if you want to stop and unload the sensor
        // but PC2MQTT will also call this if the sensor is not marked IsInitialized = true
        // Note: Cleanup is only enabled after all sensors have initialized
        public void Dispose()
        {
            Log.Debug($"Disposing [{GetSensorIdentifier()}]");
            unloadTimer = null;
            GC.SuppressFinalize(this);
        }

        // Should return "Example" (this class's name). You can specify it here as a string manually, though if you want
        public string GetSensorIdentifier() => this.GetType().Name;

        // This is called by PC2MQTT after compiling the sensor. Do your, ugh, initialization stuff here.
        // Load any databases, connect to any services, spin up any servers, etc.
        // Control will be returned to the sensor in SensorMain after all sensors have loaded.
        // Note that sensor scripts are non-blocking, so you could take all the time you want here.. but please don't :(
        public bool Initialize(SensorHost sensorHost)
        {
            // Initialize BadLogger so we can pass log messages, not required
            Log = LogManager.GetCurrentClassLogger();

            // You'll want to save this for later in the interface sensorHost object
            this.sensorHost = sensorHost;

            Log.Info($"(Initialize) CPU id: {System.Threading.Thread.GetCurrentProcessorId()} ThreadId: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            Log.Info($"IsLinux: {CSScriptLib.Runtime.IsLinux} IsWin: {CSScriptLib.Runtime.IsWin} IsCore: { CSScriptLib.Runtime.IsCore} IsMono: {CSScriptLib.Runtime.IsMono} IsNet: {CSScriptLib.Runtime.IsNet}");
            
            // Initialize needs to return true relatively quickly but you can stall for a while or run processor-intensive things beforehand.
            // Control is returned to the sensor in SensorMain after initialization of all sensors.
            Log.Info($"We're not done initializing yet.. just a bit longer..");
            System.Threading.Thread.Sleep(5000);

            Log.Info($"Finishing initialization in {this.GetSensorIdentifier()}");

            // Let PC2MQTT know that we're done and initialized properly.
            // If for some reason your code didn't initialize properly you can return false
            // Just make sure to clean up after yourself as best you can.
            // uninitialized sensors are cleaned up automatically after all sensors are done loading.
            return true;
        }

        // This is called by PC2MQTT when a topic this Sensor has subscribed to has received a message
        public void ProcessMessage(MqttMessage mqttMessage)
        {
            Log.Info($"[{GetSensorIdentifier()}] Processing topic [{mqttMessage.topic}]: {mqttMessage.message}");

            // If we receive a message for our unload topic, call sensorHost.Dispose to start the process
            if (mqttMessage.topic == "/example3/unload_example_script" && mqttMessage.message == "unload")
            {
                Log.Info("Disposing of myself in 5 seconds..");
                System.Threading.Thread.Sleep(5000);
                sensorHost.Dispose();
            }
        }

        public void SensorMain()
        {
            // We should be in our own thread. Hopefully. I'm still new to threading..
            Log.Info($"(SensorMain) CPU id: {System.Threading.Thread.GetCurrentProcessorId()} ThreadId: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            // Subscribe to JUST /example/status topic
            if (!sensorHost.Subscribe("/example1/status"))
                Log.Info("Failed to subscribe to /example/status");

            // Since this is a direct MQTT message to /example/status and we're subscribed, we should receive it
            if (!sensorHost.Publish("/example/status", "1", prependDeviceId: true, retain: false))
                Log.Info("Failed to publish to /example/status");

            // subscribe to all levels above and including /example/
            // for example, /example/hello/world/test would trigger.
            if (!sensorHost.Subscribe("/example2/#"))
                Log.Info("Failed to subscribe to /example/#");

            // We should receive both of these because /# is a multi-level wildcard
            sensorHost.Publish("/example2/test", "2", prependDeviceId: true, retain: false);
            sensorHost.Publish("/example2/test/should_also_receive/this_message", "3", prependDeviceId: true, retain: false);

            // Subscribe to all /example2/ topics one level up.
            // For example, /example2/hello would trigger
            // but /example2/hello/test would not.
            if (!sensorHost.Subscribe("/example3/+"))
                Log.Info("Failed to subscribe to /example2/+");

            // We should receive the first message but not the second, because /+ is only a single-level wildcard.
            sensorHost.Publish("/example3/test", "4", prependDeviceId: true, retain: false);
            sensorHost.Publish("/example3/test/should_not_receive", "5", prependDeviceId: true, retain: false);


            Log.Info("In 10 seconds I will send an unload message to /example2/unload_example_script");

            // Set up the timer we declared above for 10 seconds
            unloadTimer = new Timer(10000);

            // We're using a delegate here, but you could call your own method all the same
            // This will run 10 seconds after .Start() is called below
            unloadTimer.Elapsed += delegate
            {
                Log.Info("Sending unload MQTT message to myself");
                if (!sensorHost.Publish("/example3/unload_example_script", "unload"))
                    Log.Info("Failed to publish to /example3/unload_example_script");
            };
            // Start the timer
            unloadTimer.Start();


            // At any time you can unsubscribe from all topics but it's not necessary here
            // sensorHost.UnsubscribeAllTopics();

            while (this.IsInitialized)
            {
                Log.Info("If you want, you can stay in control for the life of the sensor using something like this.");
                System.Threading.Thread.Sleep(10000); // You can use a smaller sleep, just make sure you sleep to reduce CPU usage.
            }
        }

        // This is called when the Sensor is being uninitialized.
        public void Uninitialize()
        {
            // Best to check if it's already been initialized as it may get called a few times to be safe.
            if (IsInitialized)
            {
                Log.Info($"Uninitializing [{GetSensorIdentifier()}]");

                // Don't need to unsubscribe topcs as it's done by PC2MQTT
                // this._sensorHost.Unsubscribe("/example/status");
                // this._sensorHost.Unsubscribe("/example/#");
                // this._sensorHost.Unsubscribe("/example2/+");

                // stop our timer to prevent any issues!
                if (unloadTimer != null) unloadTimer.Stop();
            }
        }

        public bool IsCompatibleWithCurrentRuntime()
        {
            // Simple way to set compatibility..
            bool compatible = true;

            // If incompatible with a specific OS/Runtime, set that one below to false and remove all the others that your sensor runs on
            if (CSScriptLib.Runtime.IsCore) compatible = true;
            if (CSScriptLib.Runtime.IsLinux) compatible = true;
            if (CSScriptLib.Runtime.IsMono) compatible = true;
            if (CSScriptLib.Runtime.IsNet) compatible = true;
            if (CSScriptLib.Runtime.IsWin) compatible = true;

            return compatible;
        }
    }
}