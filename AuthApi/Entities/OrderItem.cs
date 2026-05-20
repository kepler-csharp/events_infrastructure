using System.ComponentModel.DataAnnotations.Schema;

namespace ApiGeneral.AuthApi.Entities;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public int SeatId  { get; set; }

    [Column(TypeName = "decimal(10,2)")] public decimal PricePaid { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order  Order  { get; set; } = null!;
    public Seat   Seat   { get; set; } = null!;
    public Ticket? Ticket { get; set; }
}