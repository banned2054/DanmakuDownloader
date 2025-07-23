using Newtonsoft.Json;

namespace DanmakuDownloader.Models.Jellyfin;

public class JellyfinMediaFolder
{
    [JsonProperty(nameof(Name))]
    public string Name { get; set; } = string.Empty;

    [JsonProperty(nameof(Id))]
    public string Id { get; set; } = string.Empty;
}