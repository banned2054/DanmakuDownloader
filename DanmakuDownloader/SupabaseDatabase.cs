using DanmakuDownloader.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace DanmakuDownloader;

public class SupabaseDatabase : DbContext
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
        var host     = Environment.GetEnvironmentVariable("DatabaseHost");
        var port     = Environment.GetEnvironmentVariable("DatabasePort");
        var user     = Environment.GetEnvironmentVariable("DatabaseUserName");
        var password = Environment.GetEnvironmentVariable("DatabasePassword");
        var database = Environment.GetEnvironmentVariable("DatabaseName");


        var connectSetting =
            $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";

        optionsBuilder.UseNpgsql(connectSetting, opt =>
        {
            opt.EnableRetryOnFailure(3); // 最多重试3次
        });
    }
}