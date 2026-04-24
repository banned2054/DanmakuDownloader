using DanmakuDownloader.Web.Utils;
using System.Text.Json.Serialization;

namespace DanmakuDownloader.Web.Models.Jellyfin;

public class JellyfinMedia
{
    [JsonPropertyName("PremiereDate")]
    public string PremiereDateStr { get; set; } = string.Empty;

    [JsonIgnore]
    public DateTimeOffset? PremiereDate => string.IsNullOrWhiteSpace(PremiereDateStr)
        ? null
        : TimeUtils.ParseString(PremiereDateStr, "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'");

    public string Id   { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}