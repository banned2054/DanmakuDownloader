using DanmakuDownloader.Models.Config;
using DanmakuDownloader.Models.Database;
using DanmakuDownloader.Sql;
using Microsoft.EntityFrameworkCore;

namespace DanmakuDownloader.Services;

public class HotUpdateService(
    ConfigService             configService,
    IServiceScopeFactory      scopeFactory,
    ILogger<HotUpdateService> logger)
    : UpdateServiceBase<Episode>(configService, scopeFactory, logger)
{
    protected override string DefaultCronExpression => "45 */2 * * *";

    protected override string UpdateTypeName => "热更新";

    protected override string? GetCronExpressionFromConfig(TimeConfig? timeConfig)
    {
        return timeConfig?.HotUpdateCronExp;
    }

    protected override DbSet<Episode> GetEpisodeDbSet(SupabaseDatabase db)
    {
        return db.EpisodeList;
    }
}
