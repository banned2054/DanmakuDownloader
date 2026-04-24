using System.Text.Json.Serialization;

namespace DanmakuDownloader.Web.Models.Jellyfin;

public class JellyfinMediaResponse
{
    [JsonPropertyName("Items")]
    public List<JellyfinMedia>? MediaList { get; set; }
}