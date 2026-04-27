using DanmakuDownloader.Models.Danmaku;
using DanmakuDownloader.Services;
using Microsoft.AspNetCore.Mvc;

namespace DanmakuDownloader.Controllers;

[ApiController]
[Route("api/v1/danmaku-filter")]
public class DanmakuFilterController(DanmakuFilterService filterService) : ControllerBase
{
    [HttpGet]
    public List<DanmakuFilter> GetFilters()
    {
        return filterService.GetRules();
    }

    [HttpPut]
    public IActionResult SetFilters([FromBody] List<DanmakuFilter> newFilters)
    {
        filterService.UpdateRules(newFilters);
        return NoContent();
    }

    [HttpPost]
    public IActionResult InsertFilters([FromBody] List<DanmakuFilter> filters)
    {
        filterService.InsertRules(filters);
        return NoContent();
    }

    [HttpPost("delete")]
    public IActionResult DeleteFilters([FromBody] List<DanmakuFilter> filters)
    {
        filterService.DeleteRules(filters);
        return NoContent();
    }
}
