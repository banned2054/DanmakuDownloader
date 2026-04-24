using DanmakuDownloader.Web.Utils;

namespace DanmakuDownloader.Test;

public class Tests
{
    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("JellyfinUrl") ??
                                             throw new InvalidOperationException("Jellyfin Url is Null");

    private static readonly string UserName = Environment.GetEnvironmentVariable("JellyfinUserName") ??
                                              throw new InvalidOperationException("Jellyfin User Name is Null");

    private static readonly string Password = Environment.GetEnvironmentVariable("JellyfinPassword") ??
                                              throw new InvalidOperationException("Jellyfin Password is Null");

    [SetUp]
    public void Setup()
    {
        DotNetEnv.Env.Load();
    }

    [Test]
    public async Task Test1()
    {
        var jellyfin = new JellyfinUtils(BaseUrl, UserName, Password);
        await jellyfin.Login();
        await jellyfin.GetMediaFolderList();
    }
}