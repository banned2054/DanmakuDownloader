using System.Text.Json.Serialization;

namespace DanmakuDownloader.Models.Jellyfin;

public class JellyfinMediaResponse
{
    [JsonPropertyName("Items")]
    public List<JellyfinMedia>? MediaList { get; set; }
}