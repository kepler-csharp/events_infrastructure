using ApiGeneral.AuthApi.DTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IShowtimeService
{
    Task<PagedResult<ShowtimeDto>> GetAllAsync(int page, int pageSize, int? eventId);
    Task<ShowtimeDto?> GetByIdAsync(int id);
    Task<ShowtimeDto> CreateAsync(CreateShowtimeRequest request);
    Task<List<SeatDto>> GetSeatsAsync(int showtimeId);
}