using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TERMINAL_FREQUENCY.Config.Settings
{
    public static class SettingsManager
    {
        public static string DefaultFileName = "settings.json";

        private class SafeStringEnumConverter : StringEnumConverter
        {
            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                try
                {
                    return base.ReadJson(reader, objectType, existingValue, serializer);
                }
                catch
                {
                    return Activator.CreateInstance(objectType);
                }
            }
        }

        public static string GetDefaultPath()
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                string devPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", DefaultFileName);
                return Path.GetFullPath(devPath);
            }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultFileName);
        }

        public static void Save(Settings settings, string? filePath = null)
        {
            filePath ??= GetDefaultPath();

            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };

            string json = JsonConvert.SerializeObject(settings, jsonSettings);
            File.WriteAllText(filePath, json);
        }

        public static Settings Load(string? filePath = null)
        {
            filePath ??= GetDefaultPath();
            var defaults = new Settings();

            if (!File.Exists(filePath))
                Save(defaults, filePath);

            var jsonSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new SafeStringEnumConverter() }
            };

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Settings>(json, jsonSettings) ?? defaults;
        }
    }
}