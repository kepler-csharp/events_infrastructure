using ApiGeneral.AuthApi.DTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(string userId, CreateOrderRequest request);
    Task<PaymentResultDto> PayAsync(string userId, PayOrderRequest request);
    Task<PagedResult<OrderDto>> GetUserOrdersAsync(string userId, int page, int pageSize);
    Task<OrderDto?> GetByIdAsync(int id, string userId);
}