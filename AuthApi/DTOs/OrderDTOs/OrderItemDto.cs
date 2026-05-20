namespace ApiGeneral.AuthApi.DTOs.OrderDTOs;

public class OrderItemDto
{
    public int     Id             { get; set; }
    public string  SeatLabel      { get; set; } = string.Empty;
    public string  EventName      { get; set; } = string.Empty;
    public DateTime ShowtimeStart { get; set; }
    public decimal PricePaid      { get; set; }
    public string? QRCode         { get; set; }
}