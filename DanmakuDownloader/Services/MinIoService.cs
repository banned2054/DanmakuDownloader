using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace DanmakuDownloader.Services;

public class MinIoService(ConfigService configService)
{
    public async Task DownloadFromR2Async(string objectPath, string localFilePath)
    {
        // 动态获取最新配置
        var r2Conf = configService.Current?.R2Conf;
        if (r2Conf == null || string.IsNullOrWhiteSpace(r2Conf.Endpoint))
        {
            throw new Exception("R2 配置未就绪");
        }

        var minio = new MinioClient()
                   .WithEndpoint(r2Conf.Endpoint.Replace("https://", "").Replace("http://", ""))
                   .WithCredentials(r2Conf.Access, r2Conf.Secret)
                   .WithSSL()
                   .Build();
        try
        {
            // 判断对象是否存在
            await minio.StatObjectAsync(new StatObjectArgs()
                                       .WithBucket(r2Conf.Bucket)
                                       .WithObject(objectPath));
        }
        catch (ObjectNotFoundException)
        {
            return;
        }
        catch (Exception)
        {
            return;
        }

        // 下载对象
        await minio.GetObjectAsync(new GetObjectArgs()
                                  .WithBucket(r2Conf.Bucket)
                                  .WithObject(objectPath)
                                  .WithFile(localFilePath));
    }
}
