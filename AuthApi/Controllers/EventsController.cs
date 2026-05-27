using ApiGeneral.AuthApi.DTOs.EventDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;
    public EventsController(IEventService events) => _events = events;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = true)
    {
        var result = await _events.GetAllAsync(page, pageSize, isActive);
        return Ok(ApiResponse<PagedResult<EventDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _events.GetByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Event not found."));
        return Ok(ApiResponse<EventDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var result = await _events.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<EventDto>.Ok(result, "Event created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        var result = await _events.UpdateAsync(id, request);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Event not found."));
        return Ok(ApiResponse<EventDto>.Ok(result, "Event updated."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _events.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Event not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Event deactivated."));
    }
    
    /// <summary>
    /// Sube o reemplaza la imagen/póster del evento en MinIO.
    /// Content-Type: multipart/form-data, campo: file
    /// </summary>
    [HttpPost("{id:int}/upload-photo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded."));

        var result = await _events.UploadPhotoAsync(id, file);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Event not found."));

        return Ok(ApiResponse<EventDto>.Ok(result, "Event photo uploaded."));
    }

    /// <summary>Estadísticas de ocupación y ventas de un evento (solo Admin).</summary>
    [HttpGet("{id:int}/stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStats(int id)
    {
        var result = await _events.GetStatsAsync(id);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Event not found."));
        return Ok(ApiResponse<EventStatsDto>.Ok(result));
    }
}