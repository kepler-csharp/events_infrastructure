namespace ApiGeneral.AuthApi.DTOs.SeatDTOs;

public class ReserveSeatsRequest
{
    public int       ShowtimeId { get; set; }
    public List<int> SeatIds    { get; set; } = new();
}