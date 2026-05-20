using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.DTOs.VenueDTOs;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Controllers;

[ApiController]
[Route("api/venues")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venues;
    public VenuesController(IVenueService venues) => _venues = venues;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _venues.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<VenueDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _venues.GetByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Venue not found."));
        return Ok(ApiResponse<VenueDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateVenueRequest request)
    {
        var result = await _venues.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<VenueDto>.Ok(result, "Venue created."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _venues.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Venue not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Venue deactivated."));
    }
}