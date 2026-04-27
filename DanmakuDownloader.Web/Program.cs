using DanmakuDownloader.Web.Services;
using DanmakuDownloader.Web.Sql;
using Microsoft.EntityFrameworkCore;

namespace DanmakuDownloader.Web;

internal class WebProgram
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        InitBuilder(builder);
        var app = builder.Build();
        await InitApplication(app);
        await app.RunAsync();
    }

    private static void InitBuilder(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ConfigService>();
        builder.Services.AddSingleton<DanmakuFilterService>();

        builder.Services.AddSingleton<MinIoService>();
        builder.Services.AddSingleton<DanmakuService>();

        builder.Services.AddHttpClient<JellyfinService>();
        builder.Services.AddDbContext<SupabaseDatabase>();
        builder.Services.AddDbContext<LocalDatabase>();

        builder.Services.AddHostedService<ColdUpdateService>();
        builder.Services.AddHostedService<HotUpdateService>();
        builder.Services.AddHostedService<DanmakuJobExecutorService>();

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
    }

    private static async Task InitApplication(WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        var lifetime      = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var configService = app.Services.GetRequiredService<ConfigService>();
        var filterService = app.Services.GetRequiredService<DanmakuFilterService>();

        lifetime.ApplicationStopping.Register(() =>
        {
            Console.WriteLine("应用正在关闭，正在保存配置到本地...");
            configService.SaveToDisk();
            filterService.SaveToDisk();
        });

        using var scope     = app.Services.CreateScope();
        var       dbContext = scope.ServiceProvider.GetRequiredService<LocalDatabase>();
        await dbContext.Database.MigrateAsync();
    }
}