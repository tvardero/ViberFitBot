using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ViberFitBot.ViberApi.Models;
using ViberFitBot.ViberApi.Resources;

namespace ViberFitBot.ViberApi.Services;

public class TrackServiceWithDbProcedures : ITrackService
{
    public TrackServiceWithDbProcedures(TrackContext ctx)
    {
        _ctx = ctx;
    }

    public async Task AddTrackDataAsync(string imei, decimal lat, decimal lon, int typeSource = 1)
    {
        if (string.IsNullOrWhiteSpace(imei)) throw new ArgumentNullException(nameof(imei));
        if (Math.Abs(lat) > 180m) throw new ArgumentException("Incorrect coordinate", nameof(lat));
        if (Math.Abs(lon) > 180m) throw new ArgumentException("Incorrect coordinate", nameof(lon));

        await _ctx.Database.ExecuteSqlRawAsync("EXEC AddTrackData @imei @lat @lon @type;", imei, lat, lon, typeSource);
    }

    public Task<TrackStatistics> GetStatisticsOfPeriod(string imei, DateTime perionStart, DateTime periodEnd)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Track>> GetTop5TracksAsync(string imei, TrackServiceSortBy sortBy = TrackServiceSortBy.Distance)
    {
        var top5 = _ctx.Tracks.FromSqlInterpolated($"EXEC SelectTop5 @imei = {imei} @sortByType = {sortBy}");

        return await top5.ToListAsync();
    }

    public Task<TrackStatistics> GetTotalStatisticsAsync(string imei)
    {
        throw new NotImplementedException();
    }

    private readonly TrackContext _ctx;
}