namespace ApiGeneral.AuthApi.DTOs.AuthDTOs;

public class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string Email    { get; set; } = string.Empty;
    public string Token    { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
