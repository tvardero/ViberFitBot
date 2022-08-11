using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ViberFitBot.ViberApi.Services;

namespace ViberFitBot.WebApi.Controllers;

[ApiController, Route("api/test")]
public class TestController : ControllerBase
{
    public TestController(TrackService service)
    {
        _service = service;
    }

    [HttpGet("Top10")]
    public async Task<IActionResult> GetTop10(string imei, TrackService.SortBy sortBy = TrackService.SortBy.Distance)
    {
        var top10 = await _service.GetTop10TracksAsync(imei, sortBy);
        return Ok(top10);
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


    private readonly TrackService _service;
}