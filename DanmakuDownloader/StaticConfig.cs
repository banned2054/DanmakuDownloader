using System.Text.RegularExpressions;

namespace DanmakuDownloader;

public partial class StaticConfig
{
    public const string RootPath     = "/anime";
    public const string JsonRulePath = "data/block.json";
    public const string LocalSqlPath = "data/data.db";
    public const string ConfigPath   = "data/config.json";

    [GeneratedRegex(@"subject/(?<id>\d+)")]
    public static partial Regex BangumiUrlRegex();
}
