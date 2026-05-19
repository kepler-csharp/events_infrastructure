using ApiGeneral.AuthApi.Entities;

namespace ApiGeneral.AuthApi.DTOs;

// ══════════════════════════════════════════════════════════════════════════════
// SHARED
// ══════════════════════════════════════════════════════════════════════════════
public class ApiResponse<T>
{
    public bool   Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T?     Data    { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string msg = "OK")
        => new() { Success = true, Message = msg, Data = data };

    public static ApiResponse<T> Fail(string msg, List<string>? errors = null)
        => new() { Success = false, Message = msg, Errors = errors ?? new() };
}

public class PagedResult<T>
{
    public List<T> Items      { get; set; } = new();
    public int     TotalCount { get; set; }
    public int     Page       { get; set; }
    public int     PageSize   { get; set; }
    public int     TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// ══════════════════════════════════════════════════════════════════════════════
// VENUE
// ══════════════════════════════════════════════════════════════════════════════
public class VenueDto
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = string.Empty;
    public string Address  { get; set; } = string.Empty;
    public string City     { get; set; } = string.Empty;
    public int    Capacity { get; set; }
    public bool   IsActive { get; set; }
}

public class CreateVenueRequest
{
    public string Name     { get; set; } = string.Empty;
    public string Address  { get; set; } = string.Empty;
    public string City     { get; set; } = string.Empty;
    public int    Capacity { get; set; }
}

// ══════════════════════════════════════════════════════════════════════════════
// EVENT
// ══════════════════════════════════════════════════════════════════════════════
public class EventDto
{
    public int       Id              { get; set; }
    public string    Name            { get; set; } = string.Empty;
    public string    Description     { get; set; } = string.Empty;
    public string?   PosterUrl       { get; set; }
    public string    VenueName       { get; set; } = string.Empty;
    public string    VenueCity       { get; set; } = string.Empty;
    public EventType Type            { get; set; }
    public int       DurationMinutes { get; set; }
    public bool      IsActive        { get; set; }
    public DateTime  CreatedAt       { get; set; }
}

public class CreateEventRequest
{
    public string    Name            { get; set; } = string.Empty;
    public string    Description     { get; set; } = string.Empty;
    public string?   PosterUrl       { get; set; }
    public int       VenueId         { get; set; }
    public EventType Type            { get; set; }
    public int       DurationMinutes { get; set; }
}

public class UpdateEventRequest
{
    public string    Name            { get; set; } = string.Empty;
    public string    Description     { get; set; } = string.Empty;
    public string?   PosterUrl       { get; set; }
    public EventType Type            { get; set; }
    public int       DurationMinutes { get; set; }
    public bool      IsActive        { get; set; }
}

// ══════════════════════════════════════════════════════════════════════════════
// SHOWTIME
// ══════════════════════════════════════════════════════════════════════════════
public class ShowtimeDto
{
    public int           Id             { get; set; }
    public int           EventId        { get; set; }
    public string        EventName      { get; set; } = string.Empty;
    public DateTime      StartTime      { get; set; }
    public DateTime      EndTime        { get; set; }
    public decimal       BasePrice      { get; set; }
    public ShowtimeStatus Status        { get; set; }
    public int           AvailableSeats { get; set; }
    public int           TotalSeats     { get; set; }
}

public class CreateShowtimeRequest
{
    public int      EventId   { get; set; }
    public DateTime StartTime { get; set; }
    public decimal  BasePrice { get; set; }
    public List<SeatRowRequest> SeatLayout { get; set; } = new();
}

public class SeatRowRequest
{
    public string   Row       { get; set; } = string.Empty;
    public int      SeatCount { get; set; }
    public SeatType Type      { get; set; } = SeatType.Standard;
}

// ══════════════════════════════════════════════════════════════════════════════
// SEAT
// ══════════════════════════════════════════════════════════════════════════════
public class SeatDto
{
    public int        Id            { get; set; }
    public string     Row           { get; set; } = string.Empty;
    public int        Number        { get; set; }
    public string     Label         { get; set; } = string.Empty;
    public SeatType   Type          { get; set; }
    public SeatStatus Status        { get; set; }
    public DateTime?  ReservedUntil { get; set; }
}

public class ReserveSeatsRequest
{
    public int       ShowtimeId { get; set; }
    public List<int> SeatIds    { get; set; } = new();
}

public class ReservationResult
{
    public bool      Success         { get; set; }
    public string    Message         { get; set; } = string.Empty;
    public List<int> ReservedSeatIds { get; set; } = new();
    public DateTime? ExpiresAt       { get; set; }
}

// ══════════════════════════════════════════════════════════════════════════════
// ORDER
// ══════════════════════════════════════════════════════════════════════════════
public class CreateOrderRequest
{
    public List<int> SeatIds { get; set; } = new();
}

public class OrderDto
{
    public int         Id           { get; set; }
    public string      UserEmail    { get; set; } = string.Empty;
    public decimal     Total        { get; set; }
    public OrderStatus Status       { get; set; }
    public DateTime    CreatedAt    { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int     Id             { get; set; }
    public string  SeatLabel      { get; set; } = string.Empty;
    public string  EventName      { get; set; } = string.Empty;
    public DateTime ShowtimeStart { get; set; }
    public decimal PricePaid      { get; set; }
    public string? QRCode         { get; set; }
}

public class PayOrderRequest
{
    public int    OrderId       { get; set; }
    public string PaymentMethod { get; set; } = "CreditCard";
}

public class PaymentResultDto
{
    public bool      Success       { get; set; }
    public string    TransactionId { get; set; } = string.Empty;
    public decimal   AmountPaid    { get; set; }
    public DateTime  PaidAt        { get; set; }
    public List<TicketSummaryDto> Tickets { get; set; } = new();
}

public class TicketSummaryDto
{
    public int      TicketId      { get; set; }
    public string   QRCode        { get; set; } = string.Empty;
    public string   SeatLabel     { get; set; } = string.Empty;
    public string   EventName     { get; set; } = string.Empty;
    public DateTime ShowtimeStart { get; set; }
}

// ══════════════════════════════════════════════════════════════════════════════
// SCANNER / TICKET VALIDATION
// ══════════════════════════════════════════════════════════════════════════════
public class ValidateTicketRequest
{
    public string QRCode     { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
}

public class ValidateTicketResult
{
    public bool   IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public TicketDetailDto? Ticket { get; set; }
}

public class TicketDetailDto
{
    public int      TicketId      { get; set; }
    public string   HolderEmail   { get; set; } = string.Empty;
    public string   EventName     { get; set; } = string.Empty;
    public string   VenueName     { get; set; } = string.Empty;
    public DateTime ShowtimeStart { get; set; }
    public string   SeatLabel     { get; set; } = string.Empty;
    public bool     WasAlreadyUsed { get; set; }
    public DateTime? UsedAt       { get; set; }
}

// ══════════════════════════════════════════════════════════════════════════════
// ADMIN DASHBOARD
// ══════════════════════════════════════════════════════════════════════════════
public class DashboardDto
{
    public decimal TotalRevenue          { get; set; }
    public int     TotalTicketsSold      { get; set; }
    public int     ActiveEvents          { get; set; }
    public decimal TodayRevenue          { get; set; }
    public int     TodayTicketsSold      { get; set; }
    public double  AverageOccupancyPct   { get; set; }
    public List<DailyRevenueDto> RevenueByDay { get; set; } = new();
    public List<TopEventDto>     TopEvents    { get; set; } = new();
}

public class DailyRevenueDto
{
    public DateTime Date        { get; set; }
    public decimal  Revenue     { get; set; }
    public int      TicketsSold { get; set; }
}

public class TopEventDto
{
    public int    EventId        { get; set; }
    public string EventName      { get; set; } = string.Empty;
    public int    TicketsSold    { get; set; }
    public decimal Revenue       { get; set; }
    public double OccupancyPct   { get; set; }
}
