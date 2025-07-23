namespace DanmakuDownloader.Models.Jellyfin;

public class JellyfinFullMedia
{
    public List<ExternalUrl> ExternalUrls { get; set; } = [];
}

public class ExternalUrl
{
    public string Name { get; set; } = string.Empty;
    public string Url  { get; set; } = string.Empty;
}