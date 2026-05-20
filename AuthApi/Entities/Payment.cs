using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.Entities;

public class Payment
{
    public int Id { get; set; }

    public int     OrderId    { get; set; }
    [MaxLength(100)] public string Provider   { get; set; } = "Simulated";
    [MaxLength(200)] public string ExternalId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")] public decimal Amount { get; set; }

    public PaymentStatus Status    { get; set; } = PaymentStatus.Pending;
    public DateTime?     PaidAt    { get; set; }
    public DateTime      CreatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}