using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ViberFitBot.ViberApi.Models;

public class Track
{
    public Guid Id { get; init; }

    [StringLength(50), Unicode(false)]
    public string Imei { get; init; } = null!;

    public DateTime StartTimeUtc { get; init; }

    public TimeSpan Duration { get; set; }

    public double DistanceMetres { get; set; }

    [ForeignKey(nameof(FirstData))]
    public int FirstDataId { get; init; } = default!;
    public TrackLocation FirstData { get; init; } = default!;

    [ForeignKey(nameof(LatestData))]
    public int LatestDataId { get; set; } = default!;
    public TrackLocation LatestData { get; set; } = default!;
}