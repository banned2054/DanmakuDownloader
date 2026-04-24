using System.Text.Json.Serialization;

namespace DanmakuDownloader.Web.Models.Jellyfin;

public class JellyfinEpisodeResponse
{
    [JsonPropertyName("Items")]
    public List<JellyfinEpisode>? EpisodeList { get; set; }
}