using Banned.Logger;
using DanmakuDownloader.Models.Jellyfin;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DanmakuDownloader.Utils;

public class JellyfinUtils
{
    private static readonly Logger Logger = new(options =>
    {
        options.Name             = "JellyfinUtils";
        options.BaseDirectory    = "logs";
        options.WriteOnConsole   = true;
        options.MinimumLevel     = StaticConfig.LoggerLevel;
        options.LogFormat        = StaticConfig.LoggerFormat;
        options.MaxFileSize      = 5 * 1024 * 1024; // 5MB
        options.MaxRetainedFiles = 7;               // Keep logs for 7 days
        options.LogFormat        = "{timestamp:yyyy-MM-dd HH:mm:ss} {level} {name}: {message}";
    });

    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("JellyfinUrl") ??
                                             throw new InvalidOperationException("Jellyfin Url is Null");

    private static readonly string UserName = Environment.GetEnvironmentVariable("JellyfinUserName") ??
                                              throw new InvalidOperationException("Jellyfin User Name is Null");

    private static readonly string Password = Environment.GetEnvironmentVariable("JellyfinPassword") ??
                                              throw new InvalidOperationException("Jellyfin Password is Null");

    private static readonly string Version    = StaticConfig.Version;
    private static readonly string DeviceName = Environment.MachineName;
    private static readonly string DeviceId   = GenerateStableDeviceId();

    private static string _token  = string.Empty;
    private static string _userId = string.Empty;

    public static string GenerateStableDeviceId()
    {
        // 可选增强唯一性（建议加 MAC 地址或其他系统参数）
        var input = $"{DeviceName}-{UserName}";

        using var sha       = SHA256.Create();
        var       hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()[..32];
    }

    public static async Task Login()
    {
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["X-Emby-Authorization"] =
                $"MediaBrowser Client=\"{StaticConfig.ApplicationName}\", Device=\"{DeviceName}\", DeviceId=\"{DeviceId}\", Version=\"{Version}\""
        };
        var body = new { Username = UserName, Pw = Password };

        var response = await NetUtils.FetchAsync($"{BaseUrl}/Users/AuthenticateByName",
                                                 headers, enableProxy : false, body : body);
        var data = JsonConvert.DeserializeObject<JellyfinLoginResponse>(response);
        if (data == null) return;
        _token  = data.AccessToken;
        _userId = data.User.Id;
    }

    public static async Task<string> Fetch(string path)
    {
        if (path.StartsWith('/'))
        {
            path = path[1..];
        }

        var headers = new Dictionary<string, string>
        {
            ["X-Emby-Authorization"] =
                $"MediaBrowser Client=\"{StaticConfig.ApplicationName}\", Device=\"{DeviceName}\", DeviceId=\"{DeviceId}\", Version=\"{Version}\", Token=\"{_token}\""
        };
        return await NetUtils.FetchAsync($"{BaseUrl}/{path}", headers);
    }

    public static async Task<List<JellyfinMediaFolder>?> GetMediaFolderList()
    {
        var response = await Fetch("/Library/MediaFolders");
        var result   = JsonConvert.DeserializeObject<JellyfinMediaFolderResponse>(response);
        return result!.MediaFolderList;
    }

    public static async Task<List<JellyfinMedia>?> GetItems(string? mediaFolderId)
    {
        var mediaFolderParam = string.IsNullOrWhiteSpace(mediaFolderId) ? string.Empty : $"ParentId={mediaFolderId}&";
        var response         = await Fetch($"/Items?{mediaFolderParam}IncludeItemTypes=Series&Recursive=true");
        response = Regex.Unescape(response);
        var result = JsonConvert.DeserializeObject<JellyfinMediaResponse>(response);
        if (result == null)
        {
            await Logger.ErrorAsync("Try to decode to class JellyfinMediaResponse, failed");
            return null;
        }

        if (result.MediaList == null)
        {
            await Logger.ErrorAsync("Media list count 0");
            return null;
        }

        var mediaList = result.MediaList.Where(e => !string.IsNullOrWhiteSpace(e.PremiereDateStr)).ToList();

        mediaList = mediaList.OrderBy(e => e.PremiereDate).ToList();

        await Logger.DebugAsync($"Media list count: {mediaList.Count}");

        return mediaList;
    }

    public static async Task<string> GetMediaBangumiUrl(string id)
    {
        var response = await Fetch($"Users/{_userId}/Items/{id}");
        var media    = JsonConvert.DeserializeObject<JellyfinFullMedia>(response);
        return media!.ExternalUrls
                     .Where(e => e.Name == "Bangumi")
                     .Select(e => e.Url.Trim())
                     .First(e => !string.IsNullOrWhiteSpace(e));
    }

    public static async Task<List<JellyfinEpisode>?> GetEpisodeList(string id)
    {
        var response = await Fetch($"/Items?ParentId={id}&IncludeItemTypes=Episode&Recursive=true");
        response = Regex.Unescape(response);
        var result = JsonConvert.DeserializeObject<JellyfinEpisodeResponse>(response);
        return result?.EpisodeList?.OrderBy(e => e.IndexNumber).ToList();
    }
}