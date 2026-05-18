using ApiGeneral.AuthApi.DTOs;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ApiGeneral.AuthApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthControllerService _service;

    public AuthController(
        IAuthControllerService service
    )
    {
        _service = service;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        return await _service.Login(dto);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var authHeader =
            HttpContext.Request.Headers.Authorization
                .ToString();

        var token =
            authHeader.Replace("Bearer ", "");

        return await _service.Logout(token);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin(
        RegisterDto dto
    )
    {
        return await _service.RegisterAdmin(dto);
    }

    [AllowAnonymous]
    [HttpPost("register-customer")]
    public async Task<IActionResult> RegisterCustomer(
        RegisterDto dto
    )
    {
        return await _service.RegisterCustomer(dto);
    }

    [Authorize(Roles = "Admin, Scanner")]
    [HttpPost("register-scanner")]
    public async Task<IActionResult> RegisterScanner(
        RegisterDto dto
    )
    {
        return await _service.RegisterScanner(dto);
    }

    [Authorize(Roles = "Admin, Receptionist")]
    [HttpPost("register-receptionist")]
    public async Task<IActionResult> RegisterReceptionist(
        RegisterDto dto
    )
    {
        return await _service.RegisterReceptionist(dto);
    }

    [Authorize]
    [HttpPost("upload-photo")]
    public async Task<IActionResult> UploadPhoto(
        IFormFile file
    )
    {
        return await _service.UploadPhoto(
            User,
            file
        );
    }
}