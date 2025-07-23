using Newtonsoft.Json;

namespace DanmakuDownloader.Models.Jellyfin;

public class JellyfinMediaResponse
{
    [JsonProperty("Items")]
    public List<JellyfinMedia>? MediaList { get; set; }
}