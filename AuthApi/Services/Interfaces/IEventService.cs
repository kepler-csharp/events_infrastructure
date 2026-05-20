using ApiGeneral.AuthApi.DTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IEventService
{
    Task<PagedResult<EventDto>> GetAllAsync(int page, int pageSize, bool? isActive);
    Task<EventDto?> GetByIdAsync(int id);
    Task<EventDto> CreateAsync(CreateEventRequest request);
    Task<EventDto?> UpdateAsync(int id, UpdateEventRequest request);
    Task<bool> DeleteAsync(int id);    
}