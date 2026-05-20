namespace ApiGeneral.AuthApi.DTOs.OrderDTOs;

public class TicketSummaryDto
{
    public int      TicketId      { get; set; }
    public string   QRCode        { get; set; } = string.Empty;
    public string   SeatLabel     { get; set; } = string.Empty;
    public string   EventName     { get; set; } = string.Empty;
    public DateTime ShowtimeStart { get; set; }
}