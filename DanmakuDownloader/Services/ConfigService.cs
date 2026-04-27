using DanmakuDownloader.Models;
using DanmakuDownloader.Models.Config;
using System.Text.Json;

namespace DanmakuDownloader.Services;

public class ConfigService
{
    private const string ConfigPath = StaticConfig.ConfigPath;

    private readonly Lock    _lock = new();
    private readonly ILogger _logger;

    public event Action? OnConfigChanged;
    public Config        Current { get; private set; }

    public ConfigService(ILogger<ConfigService> logger)
    {
        if (File.Exists(ConfigPath))
        {
            var json = File.ReadAllText(ConfigPath);
            Current = JsonSerializer.Deserialize<Config>(json) ?? new Config();
        }
        else
        {
            Current = new Config();
        }

        _logger = logger;
    }


    public void Update(Config newConfig)
    {
        lock (_lock)
        {
            Current = newConfig;
        }

        OnConfigChanged?.Invoke();
    }

    public void SaveToDisk()
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(Current, JsonSetting.Options);

            var dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                _logger.LogDebug("Create Directory");
                Directory.CreateDirectory(dir);
            }

            _logger.LogDebug("Begin write config file");
            File.WriteAllText(ConfigPath, json);
            _logger.LogDebug("Finish write config file");
        }
    }

    public bool IsReady()
    {
        var c = Current;
        return c.Jellyfin != null                              &&
               !string.IsNullOrWhiteSpace(c.Jellyfin.Url)      &&
               !string.IsNullOrWhiteSpace(c.Jellyfin.UserName) &&
               !string.IsNullOrWhiteSpace(c.Jellyfin.Password) &&
               c.Database      != null                         &&
               c.Database.Port != null                         &&
               !string.IsNullOrWhiteSpace(c.Database.Host)     &&
               !string.IsNullOrWhiteSpace(c.Database.Table)    &&
               !string.IsNullOrWhiteSpace(c.Database.Password) &&
               !string.IsNullOrWhiteSpace(c.Database.UserName) &&
               c.R2Conf != null                                &&
               !string.IsNullOrWhiteSpace(c.R2Conf.Access)     &&
               !string.IsNullOrWhiteSpace(c.R2Conf.Secret)     &&
               !string.IsNullOrWhiteSpace(c.R2Conf.Endpoint)   &&
               !string.IsNullOrWhiteSpace(c.R2Conf.Bucket);
    }
}
