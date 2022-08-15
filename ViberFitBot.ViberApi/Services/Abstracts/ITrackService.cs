using ViberFitBot.ViberApi.Models;
using ViberFitBot.ViberApi.Resources;

namespace ViberFitBot.ViberApi.Services;

public interface ITrackService
{
    Task AddTrackDataAsync(string imei, decimal lat, decimal lon, int typeSource = 1);
    Task<TrackStatistics> GetStatisticsOfPeriod(string imei, DateTime perionStart, DateTime periodEnd);
    Task<IEnumerable<Track>> GetTop5TracksAsync(string imei, TrackServiceSortBy sortBy = TrackServiceSortBy.Distance);
    Task<TrackStatistics> GetTotalStatisticsAsync(string imei);
}
