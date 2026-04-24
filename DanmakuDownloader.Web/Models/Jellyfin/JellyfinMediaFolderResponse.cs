using System.Text.Json.Serialization;

namespace DanmakuDownloader.Web.Models.Jellyfin;

public class JellyfinMediaFolderResponse
{
    [JsonPropertyName("Items")]
    public List<JellyfinMediaFolder>? MediaFolderList { get; set; }
}