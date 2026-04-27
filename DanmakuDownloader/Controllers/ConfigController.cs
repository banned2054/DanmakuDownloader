using DanmakuDownloader.Models.Config;
using DanmakuDownloader.Services;
using Microsoft.AspNetCore.Mvc;

namespace DanmakuDownloader.Controllers;

[ApiController]
[Route("api/v1/config")]
public class ConfigController(ConfigService configService) : ControllerBase
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
