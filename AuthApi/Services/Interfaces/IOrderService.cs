using ApiGeneral.AuthApi.DTOs.OrderDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.DTOs.TicketDTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(string userId, CreateOrderRequest request);
    Task<PaymentResultDto> PayAsync(string userId, PayOrderRequest request);
    Task<PagedResult<OrderDto>> GetUserOrdersAsync(string userId, int page, int pageSize);
    Task<OrderDto?> GetByIdAsync(int id, string userId);

    /// <summary>Lista todos los tickets de una orden con sus QR.</summary>
    Task<List<OrderTicketDto>?> GetOrderTicketsAsync(int orderId, string userId);

    /// <summary>Solicita un reembolso para una orden pagada.</summary>
    Task<RefundResultDto> RequestRefundAsync(int orderId, string userId, RefundRequestDto dto);
}