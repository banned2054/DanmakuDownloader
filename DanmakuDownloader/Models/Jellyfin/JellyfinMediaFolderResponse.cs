using Newtonsoft.Json;

namespace DanmakuDownloader.Models.Jellyfin;

public class JellyfinMediaFolderResponse
{
    [JsonProperty("Items")]
    public List<JellyfinMediaFolder>? MediaFolderList { get; set; }
}