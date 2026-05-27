namespace ApiGeneral.AuthApi.DTOs.AdminDTOs;

public class UserDto
{
    public string  Id        { get; set; } = string.Empty;
    public string  FullName  { get; set; } = string.Empty;
    public string  Email     { get; set; } = string.Empty;
    public string? PhotoUrl  { get; set; }
    public bool    IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class AdminRegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role     { get; set; } = "Customer"; // Customer, Scanner, Receptionist, Admin
}
