using System.ComponentModel.DataAnnotations;

namespace ApiGeneral.AuthApi.Entities;

public class TicketValidation
{
    public int Id { get; set; }

    public int  TicketId { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string DeviceInfo     { get; set; } = string.Empty;
    public bool      WasSuccessful { get; set; }
    public string?   FailureReason { get; set; }

    public Ticket Ticket { get; set; } = null!;
}