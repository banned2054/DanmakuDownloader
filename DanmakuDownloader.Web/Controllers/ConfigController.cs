using DanmakuDownloader.Web.Models.Config;
using DanmakuDownloader.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace DanmakuDownloader.Web.Controllers;

[ApiController]
[Route("api/v1/config")]
public class ConfigController(AppConfigService configService) : ControllerBase
{
    [HttpGet]
    public Config GetConfig()
    {
        return configService.Current;
    }

    [HttpPut]
    public IActionResult Update([FromBody] Config newConfig)
    {
        configService.Update(newConfig);
        return NoContent();
    }
}