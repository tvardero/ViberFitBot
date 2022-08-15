using Microsoft.EntityFrameworkCore;
using ViberFitBot.ViberApi.Models;
using ViberFitBot.ViberApi.Resources;

namespace ViberFitBot.ViberApi.Services;

public class TrackServiceWithLinqToEntities : ITrackService
{

    public const int CreateTrackWhenTimePassedMinutes = 30;

    public TrackServiceWithLinqToEntities(TrackContext ctx)
    {
        _ctx = ctx;
    }

    public static double GetDistanceBetweenPoints(TrackLocation a, TrackLocation b)
    {
        return GetDistanceBetweenPoints(
            (double)a.Latitude,
            (double)a.Longitude,
            (double)b.Latitude,
            (double)b.Longitude);
    }

    public static double GetDistanceBetweenPoints(double lat1, double lon1, double lat2, double lon2)
    {
        // Average distance from center to surface https://en.wikipedia.org/wiki/Earth_radius
        const double EarthRadius = 6371230;

        // Formula from https://www.movable-type.co.uk/scripts/latlong.html
        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δφ = (lat2 - lat1) * Math.PI / 180;
        var Δλ = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                  Math.Cos(φ1) * Math.Cos(φ2) *
                  Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);

        // In metres
        return EarthRadius * (double)(2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }

    public async Task AddTrackDataAsync(string imei, decimal lat, decimal lon, int typeSource = 1)
    {
        if (string.IsNullOrWhiteSpace(imei)) throw new ArgumentNullException(nameof(imei));
        if (Math.Abs(lat) > 180m) throw new ArgumentException("Incorrect coordinate", nameof(lat));
        if (Math.Abs(lon) > 180m) throw new ArgumentException("Incorrect coordinate", nameof(lon));

        var tl = new TrackLocation()
        {
            Imei = imei,
            Latitude = lat,
            Longitude = lon,
            DateEvent = DateTime.UtcNow,
            DateTrack = DateTime.UtcNow,
            TypeSource = typeSource
        };

        _ctx.TrackLocations.Add(tl);

        var track = await _ctx.Tracks
            .Include(t => t.FirstData)
            .Include(t => t.LatestData)
            .Where(t => t.Imei == tl.Imei)
            .OrderBy(t => t.StartTimeUtc)
            .LastOrDefaultAsync();

        if (track != null && track.LatestData.DateTrack.AddMinutes(CreateTrackWhenTimePassedMinutes) >= tl.DateTrack)
        {
            // If track exists and time passed <= 30 mins: update latest track data of the user
            track.Duration = tl.DateTrack - track.FirstData.DateTrack;
            track.DistanceMetres += GetDistanceBetweenPoints(track.LatestData, tl);

            track.LatestData = tl;

            _ctx.Tracks.Update(track);
        }
        else
        {
            // If latest track has zero duration (and zero distance as well) - delete it.
            if (track?.Duration == TimeSpan.Zero || track?.DistanceMetres == 0) _ctx.Tracks.Remove(track);

            // If track doesn't exist or time passed > 30 mins: create new track
            track = new()
            {
                Imei = tl.Imei,
                StartTimeUtc = tl.DateTrack,
                FirstData = tl,
                LatestData = tl
            };

            _ctx.Tracks.Add(track);
        }

        await _ctx.SaveChangesAsync();
    }

    public async Task<TrackStatistics> GetTotalStatisticsAsync(string imei)
    {
        var query = _ctx.Tracks
            .Where(t => t.Imei == imei)
            .Where(t => t.DistanceMetres > 0 && t.Duration > TimeSpan.Zero);

        return await GetStatisticsFromQuery(query);
    }

    public async Task<TrackStatistics> GetStatisticsOfPeriod(string imei, DateTime perionStart, DateTime periodEnd)
    {
        var query = _ctx.Tracks
            .Where(t => t.Imei == imei)
            .Where(t => t.StartTimeUtc >= perionStart && t.StartTimeUtc <= periodEnd)
            .Where(t => t.DistanceMetres > 0 && t.Duration > TimeSpan.Zero);

        return await GetStatisticsFromQuery(query);
    }

    public async Task<IEnumerable<Track>> GetTop5TracksAsync(string imei, TrackServiceSortBy sortBy = TrackServiceSortBy.Distance)
    {
        var query = _ctx.Tracks
            .Where(t => t.Imei == imei)
            .Where(t => t.DistanceMetres > 0 && t.Duration > TimeSpan.Zero);

        query = sortBy switch
        {
            TrackServiceSortBy.StartTime => query.OrderByDescending(t => t.StartTimeUtc),
            TrackServiceSortBy.Duration => query.OrderByDescending(t => t.Duration),
            TrackServiceSortBy.Distance => query.OrderByDescending(t => t.DistanceMetres),
            _ => query.OrderByDescending(t => t.Id)
        };

        return await query.Take(5).ToListAsync();
    }

    private static async Task<TrackStatistics> GetStatisticsFromQuery(IQueryable<Track> query)
    {
        var tracksCount = await query.CountAsync();

        var totalDistance = await query.SumAsync(t => t.DistanceMetres);

        var totalDuration = (await query.Select(t => t.Duration).ToListAsync()).Aggregate(
            TimeSpan.Zero,
            (acc, next) => acc += next);

        return new() { Count = tracksCount, Distance = totalDistance, Duration = totalDuration };
    }

    private readonly TrackContext _ctx;
}