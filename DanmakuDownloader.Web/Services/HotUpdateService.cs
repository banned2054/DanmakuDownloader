using Cronos;
using DanmakuDownloader.Web.Sql;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using TimeZoneConverter;

namespace DanmakuDownloader.Web.Services;

public class HotUpdateService(
    AppConfigService          configService,
    MinIoService              minioService,
    DanmakuService            danmakuService,
    IServiceScopeFactory      scopeFactory,
    ILogger<HotUpdateService> logger) : BackgroundService
{
    private CancellationTokenSource _refreshSignal = new();

    private static readonly Regex BangumiUrlRegex = new(@"subject/(?<id>\d+)");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Work(stoppingToken);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _refreshSignal.Token);

            try
            {
                var config  = configService.Current;
                var cronExp = config.Time == null ? "30 */2 * * *" : config.Time.HotUpdateCronExp;

                if (string.IsNullOrWhiteSpace(cronExp)) cronExp = "30 */2 * * *";

                try
                {
                    CronExpression.Parse(cronExp, CronFormat.Standard);
                }
                catch (CronFormatException)
                {
                    cronExp = "30 */2 * * *";
                }

                var expression = CronExpression.Parse(cronExp);

                TimeZoneInfo zone;
                try
                {
                    zone = TZConvert.GetTimeZoneInfo(config.Time == null
                                                         ? "Asia/Shanghai"
                                                         : config.Time.TimeZone ?? "Asia/Shanghai");
                }
                catch (Exception)
                {
                    zone = TZConvert.GetTimeZoneInfo("Asia/Shanghai");
                }

                var next = expression.GetNextOccurrence(DateTimeOffset.UtcNow, zone);
                if (next.HasValue)
                {
                    var delay = next.Value - DateTimeOffset.Now;
                    if (delay.TotalMilliseconds <= 0) continue;
                    await Task.Delay(delay, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    _refreshSignal = new CancellationTokenSource();
                }
            }
        }
    }

    private async Task Work(CancellationToken stoppingToken)
    {
        if (!configService.IsReady())
        {
            logger.LogDebug("配置不完整，跳过冷更新。");
            return;
        }

        using var scope = scopeFactory.CreateScope();

        var config   = configService.Current;
        var jellyfin = scope.ServiceProvider.GetRequiredService<JellyfinService>();
        jellyfin.Initialize(config.Jellyfin!.Url!,
                            config.Jellyfin!.UserName!,
                            config.Jellyfin!.Password!);
        await jellyfin.Login();
        var mediaFolderList = await jellyfin.GetMediaFolderList();
        if (mediaFolderList == null || mediaFolderList.Count == 0)
        {
            logger.LogError("media folder count: 0");
            return;
        }

        var animeId = mediaFolderList
                     .Where(e => e.Name == config.TargetMediaFolder)
                     .Select(e => e.Id)
                     .FirstOrDefault();
        if (animeId == null)
        {
            logger.LogError("Anime id not found");
            return;
        }

        logger.LogDebug("Get Media Folder id, Now try to find anime list.");
        var mediaList = await jellyfin.GetItems(animeId);
        if (mediaList == null || mediaList.Count == 0)
        {
            logger.LogError("media count: 0");
            return;
        }

        try
        {
            var db      = scope.ServiceProvider.GetRequiredService<SupabaseDatabase>();
            var hotList = await db.EpisodeList.ToListAsync(cancellationToken : stoppingToken);
            foreach (var media in mediaList)
            {
                if (stoppingToken.IsCancellationRequested) break; // 优雅退出响应
                var id    = media.Id;
                var url   = await jellyfin.GetMediaBangumiUrl(id);
                var match = BangumiUrlRegex.Match(url);
                if (!match.Success)
                {
                    logger.LogWarning("{Url} don't match bangumi url regex", url);
                    continue;
                }

                var bangumiId = int.Parse(match.Groups["id"].Value);
                if (hotList.All(e => e.SubjectId != bangumiId))
                {
                    continue;
                }

                var episodeList = hotList.Where(e => e.SubjectId == bangumiId)
                                         .Select(e => int.Parse(e.EpisodeNum.ToString(CultureInfo.CurrentCulture)));

                var fileNameList = await jellyfin.GetEpisodeList(id);
                var len          = fileNameList!.Count;
                foreach (var index in episodeList.Where(e => e <= len && e > 0))
                {
                    var path = $"{StaticConfig.RootPath}/[{media.PremiereDate:yyyy.MM}]{media.Name}";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    var filePath = $"{path}/{media.Name} E{fileNameList[index - 1].IndexNumber:d2}.xml";
                    await minioService.DownloadFromR2Async($"{bangumiId}/{index}.xml", filePath);
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    danmakuService.FilterDanmakuFile(filePath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
        }
        finally
        {
            await jellyfin.Logout();
        }
    }
}