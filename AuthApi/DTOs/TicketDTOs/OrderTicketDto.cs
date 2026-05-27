namespace ApiGeneral.AuthApi.DTOs.TicketDTOs;

public class OrderTicketDto
{
    public int      TicketId     { get; set; }
    public string   QRCode       { get; set; } = string.Empty;
    public string?  QrImageUrl   { get; set; }
    public string   SeatLabel    { get; set; } = string.Empty;
    public string   EventName    { get; set; } = string.Empty;
    public DateTime ShowtimeStart { get; set; }
    public bool     IsUsed       { get; set; }
    public DateTime? UsedAt      { get; set; }
}
