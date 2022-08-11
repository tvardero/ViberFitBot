using System.ComponentModel.DataAnnotations.Schema;

namespace ViberFitBot.ViberApi.Models;

[NotMapped]
public class TrackStatistics
{
    public int Count { get; init; }
    public double Distance { get; init; }
    public TimeSpan Duration { get; init; }
}
