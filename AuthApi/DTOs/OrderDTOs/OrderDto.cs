using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.DTOs.OrderDTOs;

public class OrderDto
{
    public int         Id           { get; set; }
    public string      UserEmail    { get; set; } = string.Empty;
    public decimal     Total        { get; set; }
    public OrderStatus Status       { get; set; }
    public DateTime    CreatedAt    { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}