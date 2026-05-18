using System.Security.Claims;
using ApiGeneral.AuthApi.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IAuthControllerService
{
    Task<IActionResult> Login(LoginDto dto);

    Task<IActionResult> Logout(string token);

    Task<IActionResult> RegisterAdmin(RegisterDto dto);

    Task<IActionResult> RegisterCustomer(RegisterDto dto);

    Task<IActionResult> RegisterScanner(RegisterDto dto);

    Task<IActionResult> RegisterReceptionist(RegisterDto dto);

    Task<IActionResult> UploadPhoto(
        ClaimsPrincipal principal,
        IFormFile file
    );
}