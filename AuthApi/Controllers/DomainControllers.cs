using System.Security.Claims;
using ApiGeneral.AuthApi.DTOs;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Controllers;

// ══════════════════════════════════════════════════════════════════════════════
// VENUES
// ══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/venues")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venues;
    public VenuesController(IVenueService venues) => _venues = venues;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _venues.GetAllAsync(page, pageSize);
        return Ok(ApiResponse<PagedResult<VenueDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _venues.GetByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Venue not found."));
        return Ok(ApiResponse<VenueDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateVenueRequest request)
    {
        var result = await _venues.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<VenueDto>.Ok(result, "Venue created."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _venues.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Venue not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Venue deactivated."));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// EVENTS
// ══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;
    public EventsController(IEventService events) => _events = events;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = true)
    {
        var result = await _events.GetAllAsync(page, pageSize, isActive);
        return Ok(ApiResponse<PagedResult<EventDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _events.GetByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Event not found."));
        return Ok(ApiResponse<EventDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var result = await _events.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<EventDto>.Ok(result, "Event created."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        var result = await _events.UpdateAsync(id, request);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Event not found."));
        return Ok(ApiResponse<EventDto>.Ok(result, "Event updated."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _events.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Event not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Event deactivated."));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// SHOWTIMES
// ══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/showtimes")]
public class ShowtimesController : ControllerBase
{
    private readonly IShowtimeService _showtimes;
    public ShowtimesController(IShowtimeService showtimes) => _showtimes = showtimes;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? eventId = null)
    {
        var result = await _showtimes.GetAllAsync(page, pageSize, eventId);
        return Ok(ApiResponse<PagedResult<ShowtimeDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _showtimes.GetByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<object>.Fail("Showtime not found."));
        return Ok(ApiResponse<ShowtimeDto>.Ok(result));
    }

    [HttpGet("{id:int}/seats")]
    public async Task<IActionResult> GetSeats(int id)
    {
        var result = await _showtimes.GetSeatsAsync(id);
        return Ok(ApiResponse<List<SeatDto>>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateShowtimeRequest request)
    {
        var result = await _showtimes.CreateAsync(request);
        return Created($"/api/showtimes/{result.Id}",
            ApiResponse<ShowtimeDto>.Ok(result, "Showtime created."));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// SEATS
// ══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/seats")]
[Authorize]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _seats;
    public SeatsController(ISeatService seats) => _seats = seats;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Reserve seats for 5 minutes (uses Redis lock)</summary>
    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve([FromBody] ReserveSeatsRequest request)
    {
        var result = await _seats.ReserveAsync(UserId, request);
        if (!result.Success) return Conflict(ApiResponse<ReservationResult>.Fail(result.Message));
        return Ok(ApiResponse<ReservationResult>.Ok(result, "Seats reserved for 5 minutes."));
    }

    /// <summary>Release a seat reservation</summary>
    [HttpPost("release")]
    public async Task<IActionResult> Release([FromBody] List<int> seatIds)
    {
        await _seats.ReleaseAsync(UserId, seatIds);
        return Ok(ApiResponse<object>.Ok(null!, "Seats released."));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// ORDERS
// ══════════════════════════════════════════════════════════════════════════════
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
}

// ══════════════════════════════════════════════════════════════════════════════
// SCANNER
// ══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/scanner")]
[Authorize(Roles = "Admin,Scanner,Receptionist")]
public class ScannerController : ControllerBase
{
    private readonly IScannerService _scanner;
    public ScannerController(IScannerService scanner) => _scanner = scanner;

    /// <summary>Validate a QR ticket at venue entry</summary>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateTicketRequest request)
    {
        var result = await _scanner.ValidateAsync(request);
        if (!result.IsValid)
            return BadRequest(ApiResponse<ValidateTicketResult>.Ok(result, result.Message));
        return Ok(ApiResponse<ValidateTicketResult>.Ok(result, "Access granted."));
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// ADMIN DASHBOARD
// ══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    public AdminController(IAdminService admin) => _admin = admin;

    /// <summary>Dashboard: sales, occupancy, revenue (cached 5 min in Redis)</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var result = await _admin.GetDashboardAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }
}
