using ApiGeneral.AuthApi.DTOs.Shared;

namespace ApiGeneral.AuthApi.DTOs.AdminDTOs;

public class AuditLogDto
{
    public int      Id          { get; set; }
    public string   AdminEmail  { get; set; } = string.Empty;
    public string   Action      { get; set; } = string.Empty;
    public string   EntityType  { get; set; } = string.Empty;
    public string?  EntityId    { get; set; }
    public string?  OldValues   { get; set; }
    public string?  NewValues   { get; set; }
    public DateTime CreatedAt   { get; set; }
}

public class AuditLogFilterRequest
{
    public string?   AdminEmail  { get; set; }
    public string?   Action      { get; set; }
    public DateTime? From        { get; set; }
    public DateTime? To          { get; set; }
    public int       Page        { get; set; } = 1;
    public int       PageSize    { get; set; } = 50;
}
