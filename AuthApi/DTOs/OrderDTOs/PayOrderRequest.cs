namespace ApiGeneral.AuthApi.DTOs.OrderDTOs;

public class PayOrderRequest
{
    public int    OrderId       { get; set; }
    public string PaymentMethod { get; set; } = "CreditCard";
}