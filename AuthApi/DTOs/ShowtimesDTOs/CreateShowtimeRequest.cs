namespace ApiGeneral.AuthApi.DTOs.ShowtimesDTOs;

public class CreateShowtimeRequest
{
    public int      EventId   { get; set; }
    public DateTime StartTime { get; set; }
    public decimal  BasePrice { get; set; }
    public List<SeatRowRequest> SeatLayout { get; set; } = new();
}