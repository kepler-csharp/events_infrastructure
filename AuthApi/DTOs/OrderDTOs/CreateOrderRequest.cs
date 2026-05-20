namespace ApiGeneral.AuthApi.DTOs.OrderDTOs;

public class CreateOrderRequest
{
    public List<int> SeatIds { get; set; } = new();
}