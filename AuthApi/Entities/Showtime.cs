using System.ComponentModel.DataAnnotations.Schema;
using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.Entities;

public class Showtime
{
    public int Id { get; set; }

    public int      EventId   { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime   { get; set; }

    [Column(TypeName = "decimal(10,2)")] public decimal BasePrice { get; set; }

    public ShowtimeStatus Status    { get; set; } = ShowtimeStatus.Active;
    public DateTime       CreatedAt { get; set; } = DateTime.UtcNow;

    public Event Event { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}