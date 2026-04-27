using DanmakuDownloader.Models.Database;
using DanmakuDownloader.Services;
using Microsoft.EntityFrameworkCore;

namespace DanmakuDownloader.Sql;

public class SupabaseDatabase(
    DbContextOptions<SupabaseDatabase> options,
    ConfigService                      configService) : DbContext(options)
{
    /// <summary>
    /// 热表，每小时更新
    /// </summary>
    public DbSet<Episode> EpisodeList { get; set; } = null!;

    /// <summary>
    /// 半冷，每天更新
    /// </summary>
    public DbSet<EpisodeCold> EpisodeListCold { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // 获取内存中的最新配置
        var config = configService.Current;

        if (config.Database == null                         ||
            string.IsNullOrWhiteSpace(config.Database.Host) ||
            config.Database.Port == null) return;

        var connectSetting =
            $"Host={config.Database.Host};"         +
            $"Port={config.Database.Port};"         +
            $"Database={config.Database.Table};"    +
            $"Username={config.Database.UserName};" +
            $"Password={config.Database.Password};" +
            $"SSL Mode=Require;Trust Server Certificate=true";

        optionsBuilder.UseNpgsql(connectSetting, opt => { opt.EnableRetryOnFailure(3); });
    }
}