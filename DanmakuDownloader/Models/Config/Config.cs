namespace DanmakuDownloader.Models.Config;

public class Config
{
    public R2Config?       R2Conf            { get; set; }
    public JellyfinConfig? Jellyfin          { get; set; }
    public string?         TargetMediaFolder { get; set; }
    public DatabaseConfig? Database          { get; set; }
    public TimeConfig?     Time              { get; set; }
}