namespace DanmakuDownloader.Models.Danmaku;

public class DanmakuFilter
{
    public int    Type   { get; set; } // 0: keyword, 1: regex
    public string Filter { get; set; } = string.Empty;
    public bool   Opened { get; set; }
}