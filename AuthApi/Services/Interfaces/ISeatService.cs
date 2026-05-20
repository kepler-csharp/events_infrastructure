using ApiGeneral.AuthApi.DTOs.SeatDTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface ISeatService
{
    Task<ReservationResult> ReserveAsync(string userId, ReserveSeatsRequest request);
    Task ReleaseAsync(string userId, List<int> seatIds);
}