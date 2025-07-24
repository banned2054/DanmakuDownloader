using Banned.Logger;

namespace DanmakuDownloader;

public class StaticConfig
{
    public static string   Version         => "0.0.3";
    public static string   ApplicationName => "DanmakuDownloader";
    public static string   RootPath        => "/app/data";
    public static string   JsonRulePath    => "/app/block.json";
    public static LogLevel LoggerLevel     { get; }
    public static string   LoggerFormat    => "{BaseDirectory}/{yyyy-MM-dd}.log";

    static StaticConfig()
    {
        var levelStr = Environment.GetEnvironmentVariable("LogLevel");
        if (string.IsNullOrWhiteSpace(levelStr))
        {
            LoggerLevel = LogLevel.Info;
        }
        else
        {
            levelStr = levelStr.Trim().ToLower();
            LoggerLevel = levelStr switch
            {
                "debug"   => LogLevel.Debug,
                "warning" => LogLevel.Warning,
                "info"    => LogLevel.Info,
                "error"   => LogLevel.Error,
                _         => LogLevel.Info
            };
        }
    }
}