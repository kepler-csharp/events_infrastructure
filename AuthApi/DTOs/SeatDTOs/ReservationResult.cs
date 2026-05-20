namespace ApiGeneral.AuthApi.DTOs.SeatDTOs;

public class ReservationResult
{
    public bool      Success         { get; set; }
    public string    Message         { get; set; } = string.Empty;
    public List<int> ReservedSeatIds { get; set; } = new();
    public DateTime? ExpiresAt       { get; set; }
}