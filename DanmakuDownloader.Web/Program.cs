using DanmakuDownloader.Web.Services;

namespace DanmakuDownloader.Web;

internal class WebProgram
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        InitBuilder(builder);
        var app = builder.Build();
        InitApplication(app);
        await app.RunAsync();
    }

    private static void InitBuilder(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<AppConfigService>();
        builder.Services.AddSingleton<DanmakuFilterService>();

        builder.Services.AddSingleton<MinIoService>();
        builder.Services.AddSingleton<DanmakuService>();

        builder.Services.AddHttpClient<JellyfinService>();
        builder.Services.AddDbContext<SupabaseDatabase>();

        builder.Services.AddHostedService<ColdUpdateService>();
        builder.Services.AddHostedService<HotUpdateService>();

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
    }

    private static void InitApplication(WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        var lifetime      = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var configService = app.Services.GetRequiredService<AppConfigService>();
        var filterService = app.Services.GetRequiredService<DanmakuFilterService>();

        lifetime.ApplicationStopping.Register(() =>
        {
            Console.WriteLine("应用正在关闭，正在保存配置到本地...");
            configService.SaveToDisk();
            filterService.SaveToDisk();
        });
    }
}