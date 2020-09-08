using PC2MQTT.MQTT;
using System;
using System.Diagnostics;
using System.Linq;

namespace ExtensionMethods
{
    public static class Extensions
    {
        public static string deviceId;

        public static string LinuxBashResult(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }

        public static string RemoveDeviceId(this string topic)
        {
            topic = topic.RemovePreSlash();

            if (topic.Length > deviceId.Length)
            {
                if (topic[0..deviceId.Length] == deviceId)
                    topic = topic[deviceId.Length..];

            }

            topic = topic.RemovePreSlash();

            return topic;
        }

        public static bool ValidateTopic(this string topic)
        {

            if (topic.Length == 0)
                throw new System.Exception("Topic length is zero");

            if (topic.Last() == '/')
                throw new System.Exception("Topic has a trailing slash");

            var t = topic.Split("/");

            for (int i = 0; i < t.Length; i++)
            {

                if (t.Contains("#") && t.Contains("+"))
                    throw new System.Exception($"Topic contains multiple different wildcards");

                if (t[i].Length == 0)
                    throw new System.Exception($"Topic section {i} length is zero");

                if (t[i].Contains("+") && t[i].Contains("#"))
                    throw new System.Exception($"Topic section {i} contains multiple wildcards");

                if (t[i].Contains("+") && t[i].Length > 1)
                    throw new System.Exception($"Topic section {i} contains more than just a single wildcard character");

                if (t[i].Contains("#") && t[i].Length > 1)
                    throw new System.Exception($"Topic section {i} contains more than just a single wildcard character");

                if (t[i] == "#" && i != t.Length -1)
                    throw new System.Exception($"Topic section {i} should be the last topic section in a # wildcard topic");
            }

            return true;
        }

        public static string RemovePreSlash(this string topic)
        {
            if (topic.Substring(0, 1) == "/") topic = topic[1..];
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

        public static string WindowsShellResult(this string cmd)
        {
            //var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {cmd}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }

    }
}