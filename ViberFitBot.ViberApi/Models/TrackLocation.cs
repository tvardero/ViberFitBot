using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ViberFitBot.ViberApi.Models;

[Table("TrackLocation")]
public class TrackLocation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("IMEI")]
    [StringLength(50)]
    [Unicode(false)]
    public string Imei { get; set; } = null!;

    [Column("latitude", TypeName = "decimal(12, 9)")]
    public decimal Latitude { get; set; }

    [Column("longitude", TypeName = "decimal(12, 9)")]
    public decimal Longitude { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DateEvent { get; set; }

    [Column("date_track", TypeName = "datetime")]
    public DateTime DateTrack { get; set; }

    public int TypeSource { get; set; }
}
