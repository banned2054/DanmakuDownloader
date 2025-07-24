using Banned.Logger;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace DanmakuDownloader.Utils;

public class MinIoUtils
{
    private static readonly Logger Logger = new(options =>
    {
        options.Name             = "MinIOUtils";
        options.BaseDirectory    = "logs";
        options.WriteOnConsole   = true;
        options.LogFormat        = StaticConfig.LoggerFormat;
        options.MinimumLevel     = StaticConfig.LoggerLevel;
        options.MaxFileSize      = 5 * 1024 * 1024; // 5MB
        options.MaxRetainedFiles = 7;               // Keep logs for 7 days
        options.LogFormat        = "{timestamp:yyyy-MM-dd HH:mm:ss} {level} {name}: {message}";
    });

    private static readonly string R2AccessKeyId = Environment.GetEnvironmentVariable("R2AccessKey") ??
                                                   throw new InvalidOperationException("R2 Access Key is Null");

    private static readonly string R2SecretAccessKey = Environment.GetEnvironmentVariable("R2SecretKey") ??
                                                       throw new InvalidOperationException("R2 Secret Key is Null");

    private static readonly string R2Endpoint = Environment.GetEnvironmentVariable("R2Endpoint") ??
                                                throw new InvalidOperationException("R2 Endpoint is Null");

    private static readonly string R2Bucket = Environment.GetEnvironmentVariable("R2Bucket") ??
                                              throw new InvalidOperationException("R2 Bucket is Null");

    public static async Task DownloadFromR2Async(string objectPath, string localFilePath)
    {
        await Logger.DebugAsync("Init Minio");
        var minio = new MinioClient()
                   .WithEndpoint(R2Endpoint.Replace("https://", "").Replace("http://", ""))
                   .WithCredentials(R2AccessKeyId, R2SecretAccessKey)
                   .WithSSL()
                   .Build();
        await Logger.DebugAsync("Init finish");
        try
        {
            // 判断对象是否存在
            await minio.StatObjectAsync(new StatObjectArgs()
                                       .WithBucket(R2Bucket)
                                       .WithObject(objectPath));
        }
        catch (ObjectNotFoundException)
        {
            await Logger.ErrorAsync($"❌ 对象不存在: {objectPath}，跳过");
            return;
        }
        catch (Exception ex)
        {
            await Logger.ErrorAsync($"{ex.Message}");
            return;
        }

        // 下载对象
        await Logger.DebugAsync("Begin download...");
        await minio.GetObjectAsync(new GetObjectArgs()
                                  .WithBucket(R2Bucket)
                                  .WithObject(objectPath)
                                  .WithFile(localFilePath));
        if (File.Exists(localFilePath))
        {
            await Logger.InfoAsync($"✅ 已保存到: {localFilePath}");
        }
        else
        {
            await Logger.ErrorAsync($"弹幕下载失败: {localFilePath}");
        }
    }
}