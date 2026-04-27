using System.Text.Json.Serialization;

namespace DanmakuDownloader.Models.Jellyfin;

public class JellyfinEpisodeResponse
{
    [JsonPropertyName("Items")]
    public List<JellyfinEpisode>? EpisodeList { get; set; }
}
