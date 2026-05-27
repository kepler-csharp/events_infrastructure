namespace ApiGeneral.AuthApi.DTOs.AdminDTOs;

// ── Users & Employees ─────────────────────────────────────────────────────────

public class UserAdminDto
{
    public string   Id        { get; set; } = string.Empty;
    public string   FullName  { get; set; } = string.Empty;
    public string   Email     { get; set; } = string.Empty;
    public string?  PhotoUrl  { get; set; }
    public bool     IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UpdateUserRequest
{
    public string  FullName { get; set; } = string.Empty;
    public string  Email    { get; set; } = string.Empty;
    public bool    IsActive { get; set; } = true;
}

public class AdminResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
