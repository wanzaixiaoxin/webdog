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
                if (!File.Exists(_historyFile))
                {
                    Logger.Info("No history file found, starting fresh");
                    return new List<HistoryItem>();
                }
                var json = File.ReadAllText(_historyFile);
                var result = JsonSerializer.Deserialize<List<HistoryItem>>(json);
                Logger.Info($"History loaded: {result?.Count ?? 0} items");
                return result ?? new List<HistoryItem>();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load history", ex);
                return new List<HistoryItem>();
            }
        }

        public void SaveHistory(List<HistoryItem> history)
        {
            try
            {
                var json = JsonSerializer.Serialize(history, _options);
                File.WriteAllText(_historyFile, json);
                Logger.Info($"History saved: {history.Count} items");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save history", ex);
            }
        }

        public List<EnvVariable> LoadEnvVars()
        {
            try
            {
                if (!File.Exists(_envFile))
                {
                    Logger.Info("No env file found, starting fresh");
                    return new List<EnvVariable>();
                }
                var json = File.ReadAllText(_envFile);
                var result = JsonSerializer.Deserialize<List<EnvVariable>>(json);
                Logger.Info($"Env vars loaded: {result?.Count ?? 0} items");
                return result ?? new List<EnvVariable>();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load env vars", ex);
                return new List<EnvVariable>();
            }
        }

        public void SaveEnvVars(List<EnvVariable> vars)
        {
            try
            {
                var json = JsonSerializer.Serialize(vars, _options);
                File.WriteAllText(_envFile, json);
                Logger.Info($"Env vars saved: {vars.Count} items");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save env vars", ex);
            }
        }
    }
}
