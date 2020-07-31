
using BadLogger;
using System;
using System.Timers;

// Namespaces aren't allowed, so either remove this (and its {} brackets) or leave it alone
// PC2MQTT attempts to remove namespaces automatically before compiling the sensor script
namespace PC2MQTT.Sensors
{
    // Change "Example" to whatever you want your Sensor to be. It needs to inherit from ISensor
    public class Example : PC2MQTT.Sensors.ISensor
    {
        // If we've finished initializing. Careful, wait too long and the sensor will be cleaned up by SensorManager
        public bool IsInitialized { get; set; }

        // Save this from the Initialize entrypoint method
        public SensorHost sensorHost { get; set; }

        // BadLogger from PC2MQTT so we can pass log messages, not required
        private BadLogger.BadLogger Log;

        // An example time, see more below in Initialize
        private Timer unloadTimer;

        // This should always return true. This is a simple test to see if the sensor was loaded properly
        public bool DidSensorCompile() => true;

        // You can call this directly if you want to stop and unload the sensor,
        // but PC2MQTT will also call this if the sensor is not marked IsInitialized
        public void Dispose()
        {
            Log.Debug($"Disposing [{GetSensorIdentifier()}]");
            Uninitialize();
            GC.SuppressFinalize(this);
        }

        // Should return "Example". You can specify it here as a string manually, though
        public string GetSensorIdentifier() => this.GetType().Name;

        // This is called by PC2MQTT after loading all sensor scripts, one at a time
        // Do your heavy lifting here, define timers, spin up extra threads, start servers, whatever
        // Note that Sensor scripts are blocking, which means PC2MQTT will not continue until exiting this method
        public bool Initialize(SensorHost sensorHost)
        {
            // Initialize BadLogger so we can pass log messages, not required
            Log = LogManager.GetCurrentClassLogger();

            // You'll want to save this for later in the interface sensorHost object
            this.sensorHost = sensorHost;

            // Subscribe to JUST /example/status topic
            sensorHost.Subscribe("/example/status");

            // subscribe to all levels above and including /example/
            // for example, /example/hello/world/test would trigger.
            sensorHost.Subscribe("/example/#");

            // Subscribe to all /example2/ topics one level up.
            // For example, /example2/hello would trigger
            // but /example2/hello/test would not.
            sensorHost.Subscribe("/example2/+");

            // These two will be received by this script
            sensorHost.Publish("/example/status", "1", prependDeviceId: true, retain: false);
            sensorHost.Publish("/example/uptime/status", "1", prependDeviceId: true, retain: false);

            // This one will not.. because /example2/+ only covers one topic level above itself.
            // Note that "true, false" is exactly the same as above just without argument names shown
            sensorHost.Publish("/example2/uptime/status", "1", true, false);

            Log.Info("In 10 seconds I will send an unload message to /example2/unload_example_script");

            // Set up the timer we declared above for 10 seconds
            unloadTimer = new Timer(10000);

            // We're using a delegate here, but you could call your own method all the same
            // This will run 10 seconds after .Start() is called below
            unloadTimer.Elapsed += delegate
            {
                Log.Info("Sending unload MQTT message to myself");
                sensorHost.Publish("/example2/unload_example_script", "");
            };
            // Start the timer
            unloadTimer.Start();

            // Let PC2MQTT know that we're done and initialized properly.
            // If for some reason your code didn't initialize properly you can return false
            // Just make sure to clean up after yourself
            IsInitialized = true;
            return IsInitialized;
        }

        // This is called by PC2MQTT when a topic this Sensor has subscribed to has received a message
        public void ProcessMessage(string topic, string message)
        {
            Log.Debug($"[{GetSensorIdentifier()}] Processing topic [{topic}]: {message}");

            // If we receive a message for our unload topic, call Dispose to start the process
            if (topic == "/example2/unload_example_script")
                Dispose();
        }

        // This is called when the Sensor is being uninitialized.
        public void Uninitialize()
        {
            // Best to check if it's already been initialized as it may get called a few times to be safe.
            if (IsInitialized)
            {
                Log.Debug($"Uninitializing [{GetSensorIdentifier()}]");

                // Don't need to unsubscribe topcs as it's done by PC2MQTT
                // this._sensorHost.Unsubscribe("/example/status");
                // this._sensorHost.Unsubscribe("/example/#");
                // this._sensorHost.Unsubscribe("/example2/+");

                // At any time you can unsubscribe from all topics but it's not necessary here
                this.sensorHost.UnsubscribeAllTopics();

                // stop our timer to prevent any issues!
                this.unloadTimer.Stop();

                // Finishes uninitializing this sensor, unmaps all topics, and disposes of the remaining bits
                this.sensorHost.DisposeSensor();
                IsInitialized = false;
            }
        }
    }
}