using ApiGeneral.AuthApi.DTOs.SeatDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.DTOs.ShowtimesDTOs;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Controllers;

[ApiController]
[Route("api/showtimes")]
public class ShowtimesController : ControllerBase
{
    private readonly IShowtimeService _showtimes;
    public ShowtimesController(IShowtimeService showtimes) => _showtimes = showtimes;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? eventId = null)
    {
        var result = await _showtimes.GetAllAsync(page, pageSize, eventId);
        return Ok(ApiResponse<PagedResult<ShowtimeDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _showtimes.GetByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Showtime not found."));
        return Ok(ApiResponse<ShowtimeDto>.Ok(result));
    }

    [HttpGet("{id:int}/seats")]
    public async Task<IActionResult> GetSeats(int id)
    {
        var result = await _showtimes.GetSeatsAsync(id);
        return Ok(ApiResponse<List<SeatDto>>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateShowtimeRequest request)
    {
        var result = await _showtimes.CreateAsync(request);
        return Created($"/api/showtimes/{result.Id}",
            ApiResponse<ShowtimeDto>.Ok(result, "Showtime created."));
    }
}