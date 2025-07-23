using Newtonsoft.Json;

namespace DanmakuDownloader.Models.Jellyfin;

public class JellyfinEpisodeResponse
{
    [JsonProperty("Items")]
    public List<JellyfinEpisode>? EpisodeList { get; set; }
}