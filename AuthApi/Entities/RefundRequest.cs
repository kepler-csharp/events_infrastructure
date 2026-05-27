using System.ComponentModel.DataAnnotations;
using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.Entities;

public class RefundRequest
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    [MaxLength(450)] public string RequestedByUserId { get; set; } = string.Empty;

    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;

    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    [MaxLength(1000)] public string? AdminNotes { get; set; }

    [MaxLength(450)] public string? ReviewedByAdminId { get; set; }

    public DateTime  RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt  { get; set; }

    public Order Order { get; set; } = null!;
}
