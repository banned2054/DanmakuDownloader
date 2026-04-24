using DanmakuDownloader.Web.Models.Danmaku;
using DanmakuDownloader.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace DanmakuDownloader.Web.Controllers;

[ApiController]
[Route("api/v1/danmaku")]
public class DanmakuController(DanmakuFilterService filterService) : ControllerBase
{
    [HttpGet("filter")]
    public List<DanmakuFilter> GetFilters()
    {
        return filterService.GetRules();
    }

    [HttpPut("filter")]
    public IActionResult SetFilters([FromBody] List<DanmakuFilter> newFilters)
    {
        filterService.UpdateRules(newFilters);
        return NoContent();
    }

    [HttpPost("filter/insert")]
    public IActionResult InsertFilter([FromBody] DanmakuFilter filter)
    {
        filterService.InsertRule(filter);
        return NoContent();
    }

    [HttpPost("filter/insert-batch")]
    public IActionResult InsertFilters([FromBody] List<DanmakuFilter> filters)
    {
        filterService.InsertRules(filters);
        return NoContent();
    }
}