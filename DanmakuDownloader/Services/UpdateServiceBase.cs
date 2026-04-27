using Cronos;
using DanmakuDownloader.Models.Config;
using DanmakuDownloader.Models.Database;
using DanmakuDownloader.Models.Job;
using DanmakuDownloader.Sql;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TimeZoneConverter;

namespace DanmakuDownloader.Services;

public abstract class UpdateServiceBase<TEpisode> : BackgroundService where TEpisode : class, IEpisode
{
    private CancellationTokenSource _refreshSignal = new();

    private readonly ConfigService        _configService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger              _logger;

    protected UpdateServiceBase(
        ConfigService        configService,
        IServiceScopeFactory scopeFactory,
        ILogger              logger)
    {
        _configService = configService;
        _scopeFactory  = scopeFactory;
        _logger        = logger;

        _configService.OnConfigChanged += HandleConfigChanged;
    }

    protected abstract string          DefaultCronExpression { get; }
    protected abstract string?         GetCronExpressionFromConfig(TimeConfig? timeConfig);
    protected abstract DbSet<TEpisode> GetEpisodeDbSet(SupabaseDatabase        db);
    protected abstract string          UpdateTypeName { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Work(stoppingToken);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _refreshSignal.Token);

            try
            {
                var config  = _configService.Current;
                var cronExp = GetCronExpressionFromConfig(config.Time) ?? DefaultCronExpression;

                if (string.IsNullOrWhiteSpace(cronExp)) cronExp = DefaultCronExpression;

                try
                {
                    CronExpression.Parse(cronExp, CronFormat.Standard);
                }
                catch (CronFormatException)
                {
                    cronExp = DefaultCronExpression;
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

                    await Task.Delay(delay, linkedCts.Token);
                }
                else if (!_configService.IsReady())
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token);
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
        if (!_configService.IsReady())
        {
            _logger.LogDebug("配置不完整，跳过{UpdateType}。", UpdateTypeName);
            return;
        }

        using var scope = _scopeFactory.CreateScope();

        var config   = _configService.Current;
        var jellyfin = scope.ServiceProvider.GetRequiredService<JellyfinService>();
        jellyfin.Initialize(config.Jellyfin!.Url!,
                            config.Jellyfin!.UserName!,
                            config.Jellyfin!.Password!);
        await jellyfin.Login();
        var mediaFolderList = await jellyfin.GetMediaFolderList();
        if (mediaFolderList == null || mediaFolderList.Count == 0)
        {
            _logger.LogError("media folder count: 0");
            return;
        }

        var animeId = mediaFolderList
                     .Where(e => e.Name == config.TargetMediaFolder)
                     .Select(e => e.Id)
                     .FirstOrDefault();
        if (animeId == null)
        {
            _logger.LogError("Anime id not found");
            return;
        }

        _logger.LogDebug("Get Media Folder id, Now try to find anime list.");
        var mediaList = await jellyfin.GetItems(animeId);
        if (mediaList == null || mediaList.Count == 0)
        {
            _logger.LogError("media count: 0");
            return;
        }

        try
        {
            var supabaseDb  = scope.ServiceProvider.GetRequiredService<SupabaseDatabase>();
            var localDb     = scope.ServiceProvider.GetRequiredService<LocalDatabase>();
            var episodeList = await GetEpisodeDbSet(supabaseDb).ToListAsync(cancellationToken : stoppingToken);
            var newJobs     = new List<DanmakuJob>();

            foreach (var media in mediaList)
            {
                if (stoppingToken.IsCancellationRequested) break;

                var id    = media.Id;
                var url   = await jellyfin.GetMediaBangumiUrl(id);
                var match = StaticConfig.BangumiUrlRegex().Match(url);
                if (!match.Success)
                {
                    _logger.LogWarning("{Url} don't match bangumi url regex", url);
                    continue;
                }

                var bangumiId = int.Parse(match.Groups["id"].Value);
                if (episodeList.All(e => e.SubjectId != bangumiId))
                {
                    continue;
                }

                var episodeNumbers = episodeList.Where(e => e.SubjectId == bangumiId)
                                                .Select(e =>
                                                            int.Parse(e.EpisodeNum
                                                                       .ToString(CultureInfo.CurrentCulture)));

                var fileNameList = await jellyfin.GetEpisodeList(id);
                var len          = fileNameList!.Count;

                foreach (var index in episodeNumbers.Where(e => e <= len && e > 0))
                {
                    var path     = $"{StaticConfig.RootPath}/[{media.PremiereDate:yyyy.MM}]{media.Name}";
                    var filePath = $"{path}/{media.Name} E{fileNameList[index - 1].IndexNumber:d2}.xml";
                    var existingJob = await localDb.DanmakuJobs.FirstOrDefaultAsync(j =>
                                 j.SubjectId  == bangumiId &&
                                 j.Episode    == index     &&
                                 j.TargetPath == filePath  &&
                                 (j.Status == JobStatus.Pending || j.Status == JobStatus.Processing),
                             stoppingToken);

                    if (existingJob != null)
                    {
                        continue;
                    }

                    newJobs.Add(new DanmakuJob
                    {
                        SubjectId   = bangumiId,
                        Episode     = index,
                        TargetPath  = filePath,
                        Status      = JobStatus.Pending,
                        NextRunTime = DateTime.UtcNow
                    });
                }
            }

            if (newJobs.Count > 0)
            {
                localDb.DanmakuJobs.AddRange(newJobs);
                await localDb.SaveChangesAsync(stoppingToken);
                _logger.LogDebug("{UpdateType}创建了 {Count} 个新任务", UpdateTypeName, newJobs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{UpdateType}创建任务时发生错误", UpdateTypeName);
        }
        finally
        {
            await jellyfin.Logout();
        }
    }

    public override void Dispose()
    {
        _configService.OnConfigChanged -= HandleConfigChanged;
        _refreshSignal.Dispose();
        base.Dispose();
    }

    private void HandleConfigChanged()
    {
        _refreshSignal.Cancel();
    }
}
