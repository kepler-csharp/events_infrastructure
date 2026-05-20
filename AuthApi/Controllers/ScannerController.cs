using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.DTOs.TicketDTOs;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Controllers;

[ApiController]
[Route("api/scanner")]
[Authorize(Roles = "Admin,Scanner,Receptionist")]
public class ScannerController : ControllerBase
{
    private readonly IScannerService _scanner;
    public ScannerController(IScannerService scanner) => _scanner = scanner;

    /// <summary>Validate a QR ticket at venue entry</summary>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateTicketRequest request)
    {
        var result = await _scanner.ValidateAsync(request);
        if (!result.IsValid)
            return BadRequest(ApiResponse<ValidateTicketResult>.Ok(result, result.Message));
        return Ok(ApiResponse<ValidateTicketResult>.Ok(result, "Access granted."));
    }
}