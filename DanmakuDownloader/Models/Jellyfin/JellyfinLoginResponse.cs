namespace DanmakuDownloader.Models.Jellyfin;

public class JellyfinLoginResponse
{
    public string           AccessToken  { get; set; } = string.Empty;
    public JellyfinUserInfo User { get; set; } = new();
}
