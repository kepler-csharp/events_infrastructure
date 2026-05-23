namespace ApiGeneral.AuthApi.DTOs.AuthDTOs;

public class UserProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
}
