using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WebDog.Models;

namespace WebDog.Services
{
    public class StorageService
    {
        private readonly string _dataDir;
        private readonly string _historyFile;
        private readonly string _envFile;
        private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

        public StorageService()
        {
            _dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WebDog");
            Directory.CreateDirectory(_dataDir);
            _historyFile = Path.Combine(_dataDir, "history.json");
            _envFile = Path.Combine(_dataDir, "env.json");
        }

        public List<HistoryItem> LoadHistory()
        {
            try
            {
                if (!File.Exists(_historyFile)) return new List<HistoryItem>();
                var json = File.ReadAllText(_historyFile);
                return JsonSerializer.Deserialize<List<HistoryItem>>(json) ?? new List<HistoryItem>();
            }
            catch
            {
                return new List<HistoryItem>();
            }
        }

        public void SaveHistory(List<HistoryItem> history)
        {
            try
            {
                var json = JsonSerializer.Serialize(history, _options);
                File.WriteAllText(_historyFile, json);
            }
            catch { }
        }

        public List<EnvVariable> LoadEnvVars()
        {
            try
            {
                if (!File.Exists(_envFile)) return new List<EnvVariable>();
                var json = File.ReadAllText(_envFile);
                return JsonSerializer.Deserialize<List<EnvVariable>>(json) ?? new List<EnvVariable>();
            }
            catch
            {
                return new List<EnvVariable>();
            }
        }

        public void SaveEnvVars(List<EnvVariable> vars)
        {
            try
            {
                var json = JsonSerializer.Serialize(vars, _options);
                File.WriteAllText(_envFile, json);
            }
            catch { }
        }
    }
}
