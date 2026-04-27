using DanmakuDownloader.Models.Config;
using DanmakuDownloader.Models.Database;
using DanmakuDownloader.Sql;
using Microsoft.EntityFrameworkCore;

namespace DanmakuDownloader.Services;

public class ColdUpdateService(
    ConfigService              configService,
    IServiceScopeFactory       scopeFactory,
    ILogger<ColdUpdateService> logger)
    : UpdateServiceBase<EpisodeCold>(configService, scopeFactory, logger)
{
    protected override string DefaultCronExpression => "45 0 * * *";

    protected override string UpdateTypeName => "冷更新";

    protected override string? GetCronExpressionFromConfig(TimeConfig? timeConfig)
    {
        return timeConfig?.ColdUpdateCronExp;
    }

    protected override DbSet<EpisodeCold> GetEpisodeDbSet(SupabaseDatabase db)
    {
        return db.EpisodeListCold;
    }
}
