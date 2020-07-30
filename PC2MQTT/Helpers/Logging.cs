using System;
using System.IO;

namespace PC2MQTT.Helpers
{
    public static class Logging
    {
        private static readonly string _path = "log.txt";

        public static void Log(string text)
        {
            if (Program.settings.config.enableLogging)
            {
                if (Program.settings.config.logToConsole)
                    Console.WriteLine(text);
                else
                    File.AppendAllText(_path, text);
            }
        }
    }
}