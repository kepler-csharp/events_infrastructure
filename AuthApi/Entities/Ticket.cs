using System.ComponentModel.DataAnnotations;

namespace ApiGeneral.AuthApi.Entities;

public class Ticket
{
    public int Id { get; set; }

    public int OrderItemId { get; set; }

    [MaxLength(500)] public string QRCode { get; set; } = string.Empty;

    public bool      IsUsed    { get; set; } = false;
    public DateTime? UsedAt    { get; set; }
    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;

    public OrderItem OrderItem { get; set; } = null!;
    public ICollection<TicketValidation> Validations { get; set; } = new List<TicketValidation>();
}