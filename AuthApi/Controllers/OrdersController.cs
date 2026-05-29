using System.Security.Claims;
using ApiGeneral.AuthApi.DTOs.OrderDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.DTOs.TicketDTOs;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    public OrdersController(IOrderService orders) => _orders = orders;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Create order from reserved seats</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _orders.CreateAsync(UserId, request);
        return Created($"/api/orders/{result.Id}",
            ApiResponse<OrderDto>.Ok(result, "Order created. Proceed to payment."));
    }

    /// <summary>Pay an order (simulated payment → generates QR tickets)</summary>
    [HttpPost("pay")]
    public async Task<IActionResult> Pay([FromBody] PayOrderRequest request)
    {
        var result = await _orders.PayAsync(UserId, request);
        return Ok(ApiResponse<PaymentResultDto>.Ok(result, "Payment successful. Tickets generated."));
    }

    /// <summary>List my orders</summary>
    [HttpGet]
    public async Task<IActionResult> MyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _orders.GetUserOrdersAsync(UserId, page, pageSize);
        return Ok(ApiResponse<PagedResult<OrderDto>>.Ok(result));
    }

    /// <summary>Get a specific order</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _orders.GetByIdAsync(id, UserId);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Order not found."));
        return Ok(ApiResponse<OrderDto>.Ok(result));
    }

    /// <summary>List all tickets (with QR) for a specific order</summary>
    [HttpGet("{id:int}/tickets")]
    public async Task<IActionResult> GetOrderTickets(int id)
    {
        var result = await _orders.GetOrderTicketsAsync(id, UserId);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Order not found."));
        return Ok(ApiResponse<List<OrderTicketDto>>.Ok(result));
    }

    /// <summary>Request a refund for a paid order (goes into Admin approval flow)</summary>
    [HttpPost("{id:int}/refund")]
    public async Task<IActionResult> RequestRefund(
        int id,
        [FromBody] RefundRequestDto dto)
    {
        try
        {
            var result = await _orders.RequestRefundAsync(
                id,
                UserId,
                dto);

            return Ok(
                ApiResponse<RefundResultDto>.Ok(
                    result,
                    "Solicitud de reembolso enviada. Pendiente de revisión por el administrador."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(
                ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                ApiResponse<object>.Fail(
                    $"Internal error: {ex.Message}"));
        }
    }
}