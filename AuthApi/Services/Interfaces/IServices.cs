using ApiGeneral.AuthApi.DTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IVenueService
{
    Task<PagedResult<VenueDto>> GetAllAsync(int page, int pageSize);
    Task<VenueDto?> GetByIdAsync(int id);
    Task<VenueDto> CreateAsync(CreateVenueRequest request);
    Task<bool> DeleteAsync(int id);
}

public interface IEventService
{
    Task<PagedResult<EventDto>> GetAllAsync(int page, int pageSize, bool? isActive);
    Task<EventDto?> GetByIdAsync(int id);
    Task<EventDto> CreateAsync(CreateEventRequest request);
    Task<EventDto?> UpdateAsync(int id, UpdateEventRequest request);
    Task<bool> DeleteAsync(int id);
}

public interface IShowtimeService
{
    Task<PagedResult<ShowtimeDto>> GetAllAsync(int page, int pageSize, int? eventId);
    Task<ShowtimeDto?> GetByIdAsync(int id);
    Task<ShowtimeDto> CreateAsync(CreateShowtimeRequest request);
    Task<List<SeatDto>> GetSeatsAsync(int showtimeId);
}

public interface ISeatService
{
    Task<ReservationResult> ReserveAsync(string userId, ReserveSeatsRequest request);
    Task ReleaseAsync(string userId, List<int> seatIds);
}

public interface IOrderService
{
    Task<OrderDto> CreateAsync(string userId, CreateOrderRequest request);
    Task<PaymentResultDto> PayAsync(string userId, PayOrderRequest request);
    Task<PagedResult<OrderDto>> GetUserOrdersAsync(string userId, int page, int pageSize);
    Task<OrderDto?> GetByIdAsync(int id, string userId);
}

public interface IScannerService
{
    Task<ValidateTicketResult> ValidateAsync(ValidateTicketRequest request);
}

public interface IAdminService
{
    Task<DashboardDto> GetDashboardAsync();
}
