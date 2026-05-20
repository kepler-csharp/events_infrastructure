using ApiGeneral.AuthApi.DTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IVenueService
{
    Task<PagedResult<VenueDto>> GetAllAsync(int page, int pageSize);
    Task<VenueDto?> GetByIdAsync(int id);
    Task<VenueDto> CreateAsync(CreateVenueRequest request);
    Task<bool> DeleteAsync(int id);
}