using Banned.Logger;
using DanmakuDownloader.Utils;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DanmakuDownloader;

internal class Program
{
    private static readonly Logger Logger = new(options =>
    {
        options.Name             = "Program";
        options.BaseDirectory    = "logs";
        options.WriteOnConsole   = true;
        options.LogFormat        = StaticConfig.LoggerFormat;
        options.MinimumLevel     = StaticConfig.LoggerLevel;
        options.MaxFileSize      = 5 * 1024 * 1024; // 5MB
        options.MaxRetainedFiles = 7;               // Keep logs for 7 days
        options.LogFormat        = "{timestamp:yyyy-MM-dd HH:mm:ss} {level} {name}: {message}";
    });

    private static readonly Regex BangumiUrlRegex = new(@"subject/(?<id>\d+)");

    private static readonly string TargetMediaFolder = Environment.GetEnvironmentVariable("TargetMediaFolder") ??
                                                       throw new InvalidOperationException("Danmaku Root Path is Null");

    private static async Task Main()
    {
        DotNetEnv.Env.Load();
        if (!Directory.Exists("logs"))
        {
            Directory.CreateDirectory("logs");
        }

        while (true)
        {
            var now = TimeUtils.GetNow();

            if (now is { Hour: 0, Minute: 45 })
            {
                await Logger.InfoAsync($"[{now}] 执行每日任务");
                _ = Task.Run(DownloadColdDanmaku);
                await DownloadColdDanmaku();
            }
            else if (now.Minute == 50)
            {
                await Logger.InfoAsync($"[{now}] 执行每小时任务");
                _ = Task.Run(DownloadHotDanmaku);
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    private static async Task DownloadHotDanmaku()
    {
        await JellyfinUtils.Login();
        var mediaFolderList = await JellyfinUtils.GetMediaFolderList();
        if (mediaFolderList == null || mediaFolderList.Count == 0)
        {
            await Logger.ErrorAsync("media folder count: 0");
            return;
        }

        var animeId = mediaFolderList
                     .Where(e => e.Name == TargetMediaFolder)
                     .Select(e => e.Id)
                     .FirstOrDefault();
        if (animeId == null)
        {
            await Logger.ErrorAsync("Anime id not found");
            return;
        }

        await Logger.DebugAsync("Get Media Folder id, Now try to find anime list.");
        var mediaList = await JellyfinUtils.GetItems(animeId);
        if (mediaList == null || mediaList.Count == 0)
        {
            await Logger.ErrorAsync("media count: 0");
            return;
        }

        try
        {
            var db      = new SupabaseDatabase();
            var hotList = await db.EpisodeList.ToListAsync();
            foreach (var media in mediaList)
            {
                var id    = media.Id;
                var url   = await JellyfinUtils.GetMediaBangumiUrl(id);
                var match = BangumiUrlRegex.Match(url);
                if (!match.Success)
                {
                    await Logger.WarningAsync($"{url} don't match bangumi url regex");
                    continue;
                }

                var bangumiId = int.Parse(match.Groups["id"].Value);
                if (hotList.All(e => e.SubjectId != bangumiId))
                {
                    continue;
                }

                var episodeList = hotList.Where(e => e.SubjectId == bangumiId)
                                         .Select(e => int.Parse(e.EpisodeNum.ToString(CultureInfo.CurrentCulture)));

                var fileNameList = await JellyfinUtils.GetEpisodeList(id);
                var len          = fileNameList!.Count;
                foreach (var index in episodeList.Where(e => e <= len && e > 0))
                {
                    var path = $"{StaticConfig.RootPath}/[{media.PremiereDate:yyyy.MM}]{media.Name}";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    var filePath = $"{path}/{media.Name} E{fileNameList[index - 1].IndexNumber:d2}.xml";
                    await MinIoUtils.DownloadFromR2Async($"{bangumiId}/{index}.xml", filePath);
                    await DanmakuUtils.Filter(filePath);
                }
            }

            await db.DisposeAsync();
        }
        catch (Exception ex)
        {
            await Logger.ErrorAsync(ex.ToString());
        }
    }

    private static async Task DownloadColdDanmaku()
    {
        await JellyfinUtils.Login();
        var mediaFolderList = await JellyfinUtils.GetMediaFolderList();
        if (mediaFolderList == null || mediaFolderList.Count == 0)
        {
            await Logger.ErrorAsync("media folder count: 0");
            return;
        }

        var animeId = mediaFolderList
                     .Where(e => e.Name == TargetMediaFolder)
                     .Select(e => e.Id)
                     .FirstOrDefault();
        if (animeId == null)
        {
            await Logger.ErrorAsync("Anime id not found");
            return;
        }

        await Logger.DebugAsync("Get Media Folder id, Now try to find anime list.");
        var mediaList = await JellyfinUtils.GetItems(animeId);
        if (mediaList == null || mediaList.Count == 0)
        {
            await Logger.ErrorAsync("media count: 0");
            return;
        }

        try
        {
            var db       = new SupabaseDatabase();
            var coldList = await db.EpisodeListCold.ToListAsync();
            foreach (var media in mediaList)
            {
                var id    = media.Id;
                var url   = await JellyfinUtils.GetMediaBangumiUrl(id);
                var match = BangumiUrlRegex.Match(url);
                if (!match.Success)
                {
                    await Logger.WarningAsync($"{url} don't match bangumi url regex");
                    continue;
                }

                var bangumiId = int.Parse(match.Groups["id"].Value);
                if (coldList.All(e => e.SubjectId != bangumiId))
                {
                    continue;
                }

                var episodeList = coldList.Where(e => e.SubjectId == bangumiId)
                                          .Select(e => int.Parse(e.EpisodeNum.ToString(CultureInfo.CurrentCulture)));

                var fileNameList = await JellyfinUtils.GetEpisodeList(id);
                var len          = fileNameList!.Count;
                foreach (var index in episodeList.Where(e => e <= len && e > 0))
                {
                    var path = $"{StaticConfig.RootPath}/[{media.PremiereDate:yyyy.MM}]{media.Name}";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    var filePath = $"{path}/{media.Name} E{fileNameList[index - 1].IndexNumber:d2}.xml";
                    await MinIoUtils.DownloadFromR2Async($"{bangumiId}/{index}.xml", filePath);
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    await DanmakuUtils.Filter(filePath);
                }
            }

            await db.DisposeAsync();
        }
        catch (Exception ex)
        {
            await Logger.ErrorAsync(ex.ToString());
        }
    }
}