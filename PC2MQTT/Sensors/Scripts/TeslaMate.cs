﻿using BadLogger;
using PC2MQTT.MQTT;
using System;
using System.Collections.Concurrent;

namespace PC2MQTT.Sensors
{
    public class TeslaMate : SensorBase, PC2MQTT.Sensors.ISensor
    {
        private ConcurrentDictionary<string, string> _cars = new ConcurrentDictionary<string, string>();
        
        public new void ProcessMessage(MqttMessage mqttMessage)
        {
            var topic = mqttMessage.GetTopicWithoutDeviceId().Split('/');

            if (topic == null || topic.Length <= 3)
                return;
            var t = string.Join('/', topic[0..2]);

            if (t == "teslamate/cars")
                HandleCommand(topic[2..], mqttMessage.message);
            else
                Log.Info($"[ProcessMessage] Unknown topic [{mqttMessage.GetRawTopic()}]");
        }

        public new void SensorMain()
        {
            Log.Info($"(SensorMain) CPU id: {System.Threading.Thread.GetCurrentProcessorId()} ThreadId: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            sensorHost.Subscribe(MqttMessageBuilder.
                NewMessage().
                SubscribeMessage.
                AddTopic("teslamate").
                AddTopic("cars").
                AddMultiLevelWildcard.
                DoNotRetain.
                QueueMessage.
                Build());
        }

        private string GetCarName(string id)
        {
            _cars.TryGetValue(id, out var name);

            if (name == null)
            {
                name = sensorHost.LoadData("CarName-" + id, id, type: typeof(string));
                SetCarName(id, name);
            }

            return name;
        }

        private void HandleCommand(string[] command, string message)
        {
            Log.Info($"[Update] {GetCarName(command[0])} {command[^1]}: {message}");

            switch (command[^1])
            {
                case "display_name":
                    SetCarName(command[0], message);
                    break;

                case "spoiler_type":
                    break;

                case "version":
                    break;

                case "latitude":
                    break;

                case "time_to_full_charge":
                    break;

                case "odometer":
                    break;

                case "is_user_present":
                    break;

                case "frunk_open":
                    break;

                case "is_climate_on":
                    break;

                case "plugged_in":
                    break;

                case "wheel_type":
                    break;

                case "charger_actual_current":
                    break;

                case "sentry_mode":
                    break;

                case "windows_open":
                    break;

                case "is_preconditioning":
                    break;

                case "charger_phases":
                    break;

                case "update_available":
                    break;

                case "locked":
                    break;

                case "ideal_battery_range_km":
                    break;

                case "charge_energy_added":
                    break;

                case "charger_power":
                    break;

                case "charge_port_door_open":
                    break;

                case "est_battery_range_km":
                    break;

                case "outside_temp":
                    break;

                case "heading":
                    break;

                case "inside_temp":
                    break;

                case "charger_voltage":
                    break;

                case "exterior_color":
                    break;

                case "battery_level":
                    break;

                case "model":
                    break;

                case "since":
                    break;

                case "state":
                    break;

                case "doors_open":
                    break;

                case "healthy":
                    break;

                case "geofence":
                    break;

                case "trunk_open":
                    break;

                case "charge_limit_soc":
                    break;

                case "usable_battery_level":
                    break;

                case "longitude":
                    break;

                case "rated_battery_range_km":
                    break;

                default:
                    break;
            }
        }

        private void SetCarName(string id, string name)
        {
            var newName = name;
            _cars.TryGetValue(id, out var oldName);

            if (_cars.ContainsKey(id))
            {
                if (_cars[id] != newName)
                {
                    _cars[id] = newName;
                    sensorHost.SaveData("CarName-" + id, newName);
                }
            }
            else
            {
                _cars.TryAdd(id, name);
            }
        }
    }
}