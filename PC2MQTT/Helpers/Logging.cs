using BadLogger;
using System;
using System.IO;

namespace PC2MQTT.Helpers
{
    public static class Logging
    {
        private static readonly string _path = "log.txt";
        private static Settings _settings;

        public static void InitializeLogging(Settings settings)
        {
            _settings = settings;
            BadLogger.EventSink.OnLogEvent += EventSink_OnLogEvent;

            LogManager.SetMinimumLogLevel(settings.config.logLevel);
        }

        private static Delegate EventSink_OnLogEvent(string log)
        {
            if (_settings.config.enableLogging)
            {
                if (_settings.config.logToConsole)
                    Console.WriteLine(log);
                else
                    File.AppendAllText(_path, log);
            }

            return null;
        }

        /*
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
        */
    }
}