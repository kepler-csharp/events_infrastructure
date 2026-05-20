using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.Entities;

public class Seat
{
    public int Id { get; set; }

    public int    ShowtimeId { get; set; }
    [MaxLength(10)] public string Row    { get; set; } = string.Empty;
    public int    Number     { get; set; }
    public SeatType   Type   { get; set; } = SeatType.Standard;
    public SeatStatus Status { get; set; } = SeatStatus.Available;

    public DateTime? ReservedUntil    { get; set; }
    [MaxLength(450)] public string? ReservedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Showtime    Showtime   { get; set; } = null!;
    public OrderItem?  OrderItem  { get; set; }

    [NotMapped] public string Label => $"{Row}{Number}";
}