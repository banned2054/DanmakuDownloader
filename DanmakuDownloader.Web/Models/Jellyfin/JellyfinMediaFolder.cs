using System.Text.Json.Serialization;

namespace DanmakuDownloader.Web.Models.Jellyfin;

public class JellyfinMediaFolder
{
    [JsonPropertyName(nameof(Name))]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName(nameof(Id))]
    public string Id { get; set; } = string.Empty;
}