using System.Security.Claims;
using ApiGeneral.AuthApi.DTOs.SeatDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Controllers;

[ApiController]
[Route("api/seats")]
[Authorize]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _seats;
    public SeatsController(ISeatService seats) => _seats = seats;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Reserve seats for 5 minutes (uses Redis lock)</summary>
    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve([FromBody] ReserveSeatsRequest request)
    {
        var result = await _seats.ReserveAsync(UserId, request);
        if (!result.Success) return Conflict(ApiResponse<ReservationResult>.Fail(result.Message));
        return Ok(ApiResponse<ReservationResult>.Ok(result, "Seats reserved for 5 minutes."));
    }

    /// <summary>Release a seat reservation</summary>
    [HttpPost("release")]
    public async Task<IActionResult> Release([FromBody] List<int> seatIds)
    {
        await _seats.ReleaseAsync(UserId, seatIds);
        return Ok(ApiResponse<object>.Ok(null!, "Seats released."));
    }
}