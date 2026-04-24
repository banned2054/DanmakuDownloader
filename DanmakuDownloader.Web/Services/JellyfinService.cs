using DanmakuDownloader.Web.Models.Jellyfin;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DanmakuDownloader.Web.Services;

public class JellyfinService(HttpClient httpClient)
{
    private readonly string _version    = StaticConfig.Version;
    private readonly string _deviceName = Environment.MachineName;

    private string _token    = string.Empty;
    private string _userId   = string.Empty;
    private string _baseUrl  = string.Empty;
    private string _userName = string.Empty;
    private string _password = string.Empty;

    public void Initialize(string baseUrl, string userName, string password)
    {
        _baseUrl  = baseUrl.TrimEnd('/');
        _userName = userName;
        _password = password;
    }

    public string GenerateStableDeviceId()
    {
        // 可选增强唯一性（建议加 MAC 地址或其他系统参数）
        var input = $"{_deviceName}-{_userName}";

        using var sha       = SHA256.Create();
        var       hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()[..32];
    }

    // 3. 构建通用的 Auth Header
    private string GetAuthorizationHeader(bool includeToken = true)
    {
        var header =
            $"MediaBrowser Client=\"{StaticConfig.ApplicationName}\", Device=\"{_deviceName}\", DeviceId=\"{GenerateStableDeviceId()}\", Version=\"{_version}\"";
        if (includeToken && !string.IsNullOrEmpty(_token))
        {
            header += $", Token=\"{_token}\"";
        }

        return header;
    }

    public async Task Login()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/Users/AuthenticateByName");
        request.Headers.Add("X-Emby-Authorization", GetAuthorizationHeader(includeToken : false));

        // HttpClient 内置了很好的 JSON 序列化支持
        request.Content = JsonContent.Create(new { Username = _userName, Pw = _password });

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<JellyfinLoginResponse>();
        if (data == null) throw new Exception("Jellyfin login failed.");

        _token  = data.AccessToken;
        _userId = data.User.Id;
    }

    // 4. 增加 Logout 方法，用完就退，不占 Jellyfin 服务器端的 Session
    public async Task Logout()
    {
        if (string.IsNullOrEmpty(_token)) return;

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/Sessions/Logout");
        request.Headers.Add("X-Emby-Authorization", GetAuthorizationHeader());

        await httpClient.SendAsync(request);

        _token  = string.Empty;
        _userId = string.Empty;
    }

    private async Task<string> Fetch(string path)
    {
        var url = path.StartsWith('/') ? $"{_baseUrl}{path}" : $"{_baseUrl}/{path}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Emby-Authorization", GetAuthorizationHeader());

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<List<JellyfinMediaFolder>?> GetMediaFolderList()
    {
        var response = await Fetch("/Library/MediaFolders");
        var result   = JsonSerializer.Deserialize<JellyfinMediaFolderResponse>(response);
        Console.WriteLine(JsonSerializer.Serialize(result));
        return result!.MediaFolderList;
    }

    public async Task<List<JellyfinMedia>?> GetItems(string? mediaFolderId)
    {
        var mediaFolderParam = string.IsNullOrWhiteSpace(mediaFolderId) ? string.Empty : $"ParentId={mediaFolderId}&";
        var response         = await Fetch($"/Items?{mediaFolderParam}IncludeItemTypes=Series&Recursive=true");
        response = Regex.Unescape(response);
        var result = JsonSerializer.Deserialize<JellyfinMediaResponse>(response);
        if (result == null)
        {
            return null;
        }

        if (result.MediaList == null)
        {
            return null;
        }

        var mediaList = result.MediaList.Where(e => !string.IsNullOrWhiteSpace(e.PremiereDateStr)).ToList();
        mediaList = mediaList.OrderBy(e => e.PremiereDate).ToList();
        foreach (var media in mediaList)
        {
            Console.WriteLine(JsonSerializer.Serialize(media));
        }

        return mediaList;
    }

    public async Task<string> GetMediaBangumiUrl(string id)
    {
        var response = await Fetch($"Users/{_userId}/Items/{id}");
        var media    = JsonSerializer.Deserialize<JellyfinFullMedia>(response);
        return media!.ExternalUrls
                     .Where(e => e.Name == "Bangumi")
                     .Select(e => e.Url.Trim())
                     .First(e => !string.IsNullOrWhiteSpace(e));
    }

    public async Task<List<JellyfinEpisode>?> GetEpisodeList(string id)
    {
        var response = await Fetch($"/Items?ParentId={id}&IncludeItemTypes=Episode&Recursive=true");
        response = Regex.Unescape(response);
        var result = JsonSerializer.Deserialize<JellyfinEpisodeResponse>(response);
        return result?.EpisodeList?.OrderBy(e => e.IndexNumber).ToList();
    }
}