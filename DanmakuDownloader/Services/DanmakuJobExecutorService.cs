using DanmakuDownloader.Models.Job;
using DanmakuDownloader.Sql;
using Microsoft.EntityFrameworkCore;

namespace DanmakuDownloader.Services;

public class DanmakuJobExecutorService(
    IServiceScopeFactory               scopeFactory,
    ILogger<DanmakuJobExecutorService> logger) : BackgroundService
{
    private const int MaxRetryCount = 3;
    private const int BatchSize     = 5;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("弹幕任务执行器已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "任务执行器发生错误");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        logger.LogInformation("弹幕任务执行器已停止");
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        using var scope          = scopeFactory.CreateScope();
        var       db             = scope.ServiceProvider.GetRequiredService<LocalDatabase>();
        var       minioService   = scope.ServiceProvider.GetRequiredService<MinIoService>();
        var       danmakuService = scope.ServiceProvider.GetRequiredService<DanmakuService>();

        var jobs = await db.DanmakuJobs
                           .Where(j => j.Status == JobStatus.Pending && j.NextRunTime <= DateTime.UtcNow)
                           .OrderBy(j => j.CreatedAt)
                           .Take(BatchSize)
                           .ToListAsync(stoppingToken);

        if (jobs.Count == 0)
        {
            return;
        }

        logger.LogDebug("本次处理 {Count} 个任务", jobs.Count);

        foreach (var job in jobs)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ExecuteJobAsync(job, db, minioService, danmakuService, stoppingToken);
        }
    }

    private async Task ExecuteJobAsync(
        DanmakuJob        job,
        LocalDatabase     db,
        MinIoService      minioService,
        DanmakuService    danmakuService,
        CancellationToken stoppingToken)
    {
        var jobId     = job.Id;
        var subjectId = job.SubjectId;
        var episode   = job.Episode;

        logger.LogDebug("开始执行任务 {JobId}: SubjectId={SubjectId}, Episode={Episode}", jobId, subjectId, episode);

        try
        {
            job.Status = JobStatus.Processing;
            await db.SaveChangesAsync(stoppingToken);

            var directory = Path.GetDirectoryName(job.TargetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await minioService.DownloadFromR2Async($"{subjectId}/{episode}.xml", job.TargetPath);

            if (!File.Exists(job.TargetPath))
            {
                throw new InvalidOperationException($"下载失败，文件不存在: {job.TargetPath}");
            }

            danmakuService.FilterDanmakuFile(job.TargetPath);

            job.Status    = JobStatus.Succeeded;
            job.LastError = null;

            logger.LogInformation("任务 {JobId} 执行成功: {TargetPath}", jobId, job.TargetPath);
        }
        catch (Exception ex)
        {
            job.RetryCount++;
            job.LastError = ex.Message;

            if (job.RetryCount >= MaxRetryCount)
            {
                job.Status = JobStatus.Failed;
                logger.LogError(ex, "任务 {JobId} 执行失败，已达最大重试次数", jobId);
            }
            else
            {
                job.Status      = JobStatus.Pending;
                job.NextRunTime = DateTime.UtcNow.Add(RetryDelay);
                logger.LogWarning(ex, "任务 {JobId} 执行失败，第 {RetryCount} 次重试，将在 {NextRunTime} 再次尝试",
                                  jobId, job.RetryCount, job.NextRunTime);
            }
        }

        try
        {
            await db.SaveChangesAsync(stoppingToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("任务 {JobId} 发生并发冲突，将被其他实例处理", jobId);
        }
    }
}