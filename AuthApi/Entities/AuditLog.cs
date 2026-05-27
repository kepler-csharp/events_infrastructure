using System.ComponentModel.DataAnnotations;

namespace ApiGeneral.AuthApi.Entities;

public class AuditLog
{
    public int Id { get; set; }

    [MaxLength(450)] public string AdminUserId { get; set; } = string.Empty;
    [MaxLength(256)] public string AdminEmail  { get; set; } = string.Empty;

    /// <summary>Acción realizada, e.g. "UpdateCustomer", "DeactivateEmployee".</summary>
    [MaxLength(100)] public string Action { get; set; } = string.Empty;

    /// <summary>Tipo de entidad afectada, e.g. "Customer", "Employee", "Order".</summary>
    [MaxLength(100)] public string EntityType { get; set; } = string.Empty;

    /// <summary>ID de la entidad afectada (puede ser int o Guid en string).</summary>
    [MaxLength(100)] public string? EntityId { get; set; }

    /// <summary>Snapshot JSON antes del cambio (opcional).</summary>
    public string? OldValues { get; set; }

    /// <summary>Snapshot JSON después del cambio (opcional).</summary>
    public string? NewValues { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
