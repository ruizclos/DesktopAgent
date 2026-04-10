using System;
using System.IO;
using System.Text.Json;
using LocalAIAgent.Models;

namespace LocalAIAgent.Services
{
    public class ConfigService
    {
        private const string ConfigFileName = "appsettings.json";

        public AppConfig Config { get; private set; }

        public ConfigService()
        {
            if (!File.Exists(ConfigFileName))
            {
                Config = new AppConfig();
                Save();
            }
            else
            {
                var json = File.ReadAllText(ConfigFileName);
                Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(ConfigFileName, json);
        }
    }
}
