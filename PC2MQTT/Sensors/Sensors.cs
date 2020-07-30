using System;
using System.Collections.Generic;
using System.Text;

namespace PC2MQTT.Sensors
{
    public class ImplementedSensors
    {
        public bool volumeLevel = true;
        public bool volumeMute = true;
        public bool cpuUsage = true;
        public bool freeMemory = true;
        public bool batteryLife = true;
        public bool hdd = true;

        //public bool webcam;
        public bool monitor = true;

        //public bool screenshot = true;
        public bool idle = true;

        public bool tts = true;
        public bool audioPlayer = true;
        public bool toast = true;
        public bool sleep = true;
        public bool shutdown = true;
        public bool hibernate = true;
        public bool reboot = true;
        public bool runCommands = false;
    }
}
