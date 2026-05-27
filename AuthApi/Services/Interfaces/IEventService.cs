using ApiGeneral.AuthApi.DTOs.EventDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IEventService
{
    Task<PagedResult<EventDto>> GetAllAsync(int page, int pageSize, bool? isActive);
    Task<EventDto?> GetByIdAsync(int id);
    Task<EventDto> CreateAsync(CreateEventRequest request);
    Task<EventDto?> UpdateAsync(int id, UpdateEventRequest request);
    Task<bool> DeleteAsync(int id);

    /// <summary>Sube una imagen al bucket de eventos en MinIO y actualiza PosterUrl.</summary>
    Task<EventDto?> UploadPhotoAsync(int id, IFormFile file);

    /// <summary>Estadísticas de ocupación y ventas de un evento específico (solo Admin).</summary>
    Task<EventStatsDto?> GetStatsAsync(int eventId);
}