using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ViberFitBot.ViberApi.Resources;
using ViberFitBot.ViberApi.Services;

namespace ViberFitBot.WebApi.Controllers;

[ApiController, Route("api/test")]
public class TestController : ControllerBase
{
    public TestController(ITrackService service)
    {
        _service = service;
    }

    [HttpGet("Top5")]
    public async Task<IActionResult> GetTop5(string imei, TrackServiceSortBy sortBy = TrackServiceSortBy.Distance)
    {
        var top5 = await _service.GetTop5TracksAsync(imei, sortBy);
        return Ok(top5);
    }

    [HttpGet("StatisticsOfPeriod")]
    public async Task<IActionResult> GetStatisticsOfPeriod(string imei, DateTime periodStart, DateTime periodEnd)
    {
        var todayStats = await _service.GetStatisticsOfPeriod(imei, periodStart, periodEnd);
        return Ok(todayStats);
    }

    [HttpGet("TotalStatistics")]
    public async Task<IActionResult> GetTotalStatistics(string imei)
    {
        var totalStats = await _service.GetTotalStatisticsAsync(imei);
        return Ok(totalStats);
    }

    [HttpPost]
    public async Task<IActionResult> PostTrackData(
        [Required] string imei,
        [Range(-180d, 180d)] decimal lat,
        [Range(-180d, 180d)] decimal lon,
        int typeSource = 1)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        await _service.AddTrackDataAsync(imei, lat, lon, typeSource);
        return Ok();
    }

    private readonly ITrackService _service;
}