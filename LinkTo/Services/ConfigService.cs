using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using LinkTo.Models;

namespace LinkTo.Services;

/// <summary>
/// Configuration service for loading and saving app settings
/// </summary>
public class ConfigService
{
    private static readonly Lazy<ConfigService> _instance = new(() => new ConfigService());
    public static ConfigService Instance => _instance.Value;

    private readonly string _configPath;
    private AppConfig _config;

    public AppConfig Config => _config;

    private ConfigService()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LinkTo");

        Directory.CreateDirectory(appDataDir);
        _configPath = Path.Combine(appDataDir, "Config.json");
        _config = Load();
    }

    private AppConfig Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                LogService.Instance.LogInfo($"Read config file. Length: {json.Length}");
                
                try 
                {
                    var config = JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig);
                    if (config != null)
                    {
                        LogService.Instance.LogInfo("Configuration loaded successfully");
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    LogService.Instance.LogError("Deserialization failed", ex);
                    // Return default config but backup corrupt file? 
                    // For now just log.
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("Failed to load configuration file", ex);
        }

        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, AppConfigJsonContext.Default.AppConfig);
            File.WriteAllText(_configPath, json);
            LogService.Instance.LogInfo("Configuration saved successfully");
        }
        catch (Exception ex)
        {
            LogService.Instance.LogError("Failed to save configuration", ex);
        }
    }

    // Common directories management
    public void AddCommonDirectory(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && 
            !_config.CommonDirectories.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            _config.CommonDirectories.Add(path);
            Save();
        }
    }

    public void RemoveCommonDirectory(string path)
    {
        var index = _config.CommonDirectories.FindIndex(d => 
            string.Equals(d, path, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            _config.CommonDirectories.RemoveAt(index);
            Save();
        }
    }

    // Link history management
    public void AddLinkHistory(LinkHistoryEntry entry)
    {
        _config.LinkHistory.Insert(0, entry);
        Save();
    }

    public void RemoveLinkHistory(Guid id)
    {
        _config.LinkHistory.RemoveAll(e => e.Id == id);
        Save();
    }

    // Language
    public string Language
    {
        get => _config.Language;
        set
        {
            _config.Language = value;
            Save();
        }
    }

    // Shell menu
    public bool ShellMenuEnabled
    {
        get => _config.ShellMenuEnabled;
        set
        {
            _config.ShellMenuEnabled = value;
            Save();
        }
    }
}
