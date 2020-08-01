using System;

namespace ExtensionMethods
{
    public static class Extensions
    {
        public static string deviceId;
        public static string RemoveDeviceId(this string topic)
        {
            if (topic.Substring(0, 1) == "/") topic = topic[1..];

            if (topic.Length > deviceId.Length)
            {
                if (topic[0..deviceId.Length] == deviceId)
                    topic = topic[deviceId.Length..];
            }

            return topic;
        }

        public static string ResultantTopic(this string topic, bool prependDeviceId = true, bool removeWildcards = false)
        {
            if (topic.Substring(0, 1) == "/") topic = topic[1..];

            if (prependDeviceId)
            {
                if (topic.Length > deviceId.Length)
                {
                    if (topic[0..deviceId.Length] != deviceId)
                        topic = $"{deviceId}/{topic}";
                }
                else
                {
                    topic = $"{deviceId}/{topic}";
                }
            }

            if (removeWildcards)
                topic = topic.Replace("/#", "").Replace("/+", "");

            return topic;
        }

        public static string ToReadableFileSize(this long size)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return String.Format("{0:0.##} {1}", size, sizes[order]);
        }

    }
}