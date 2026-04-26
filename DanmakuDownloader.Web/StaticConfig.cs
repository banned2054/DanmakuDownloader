namespace DanmakuDownloader.Web;

public class StaticConfig
{
    public static string   Version         => "0.0.5";
    public static string   ApplicationName => "DanmakuDownloader";
    public static string   RootPath        => "/anime";
    public static string   JsonRulePath    => "/app/block.json";
    public static LogLevel LoggerLevel     { get; }
    public static string   LoggerFormat    => "{BaseDirectory}/{yyyy-MM-dd}.log";

    static StaticConfig()
    {
    }
}