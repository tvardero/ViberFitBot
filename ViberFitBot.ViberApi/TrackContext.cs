using Microsoft.EntityFrameworkCore;
using ViberFitBot.ViberApi.Models;

namespace ViberFitBot.ViberApi;

public class TrackContext : DbContext
{
    public DbSet<Track> Tracks { get; init; } = null!;

    public DbSet<TrackLocation> TrackLocations { get; init; } = null!;

    public TrackContext(DbContextOptions<TrackContext> options) : base(options)
    {
        Database.Migrate();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var trackLocation = modelBuilder.Entity<TrackLocation>();
        trackLocation.Property(tl => tl.TypeSource).HasDefaultValueSql("((1))");

        var track = modelBuilder.Entity<Track>();
        track.HasOne(t => t.FirstData).WithOne()
            .OnDelete(DeleteBehavior.Restrict);
        track.HasOne(t => t.LatestData).WithOne()
            .OnDelete(DeleteBehavior.Restrict);
        track.Property(t => t.Duration)
            .HasConversion(duration => duration.Ticks, ticks => TimeSpan.FromTicks(ticks)); // Because we are unable to store > 24:00:00 using "time" DbType

        base.OnModelCreating(modelBuilder);
    }
}
