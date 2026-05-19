using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiGeneral.AuthApi.Entities;

// ─── Venue ────────────────────────────────────────────────────────────────────
public class Venue
{
    public int Id { get; set; }

    [MaxLength(200)] public string Name    { get; set; } = string.Empty;
    [MaxLength(500)] public string Address { get; set; } = string.Empty;
    [MaxLength(100)] public string City    { get; set; } = string.Empty;

    public int  Capacity  { get; set; }
    public bool IsActive  { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}

// ─── Event ────────────────────────────────────────────────────────────────────
public class Event
{
    public int Id { get; set; }

    [MaxLength(200)] public string Name        { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [MaxLength(1000)] public string? PosterUrl  { get; set; }

    public int       VenueId          { get; set; }
    public EventType Type             { get; set; } = EventType.Movie;
    public int       DurationMinutes  { get; set; }
    public bool      IsActive         { get; set; } = true;
    public DateTime  CreatedAt        { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt        { get; set; }

    public Venue Venue { get; set; } = null!;
    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}

public enum EventType { Movie = 0, Concert = 1, Theater = 2, Sports = 3, Other = 4 }

// ─── Showtime ─────────────────────────────────────────────────────────────────
public class Showtime
{
    public int Id { get; set; }

    public int      EventId   { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime   { get; set; }

    [Column(TypeName = "decimal(10,2)")] public decimal BasePrice { get; set; }

    public ShowtimeStatus Status    { get; set; } = ShowtimeStatus.Active;
    public DateTime       CreatedAt { get; set; } = DateTime.UtcNow;

    public Event Event { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}

public enum ShowtimeStatus { Active = 0, Cancelled = 1, Completed = 2, SoldOut = 3 }

// ─── Seat ─────────────────────────────────────────────────────────────────────
public class Seat
{
    public int Id { get; set; }

    public int    ShowtimeId { get; set; }
    [MaxLength(10)] public string Row    { get; set; } = string.Empty;
    public int    Number     { get; set; }
    public SeatType   Type   { get; set; } = SeatType.Standard;
    public SeatStatus Status { get; set; } = SeatStatus.Available;

    public DateTime? ReservedUntil    { get; set; }
    [MaxLength(450)] public string? ReservedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Showtime    Showtime   { get; set; } = null!;
    public OrderItem?  OrderItem  { get; set; }

    [NotMapped] public string Label => $"{Row}{Number}";
}

public enum SeatType   { Standard = 0, Premium = 1, VIP = 2 }
public enum SeatStatus { Available = 0, Reserved = 1, Sold = 2 }

// ─── Order ────────────────────────────────────────────────────────────────────
public class Order
{
    public int Id { get; set; }

    [MaxLength(450)] public string UserId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")] public decimal Total { get; set; }

    public OrderStatus Status    { get; set; } = OrderStatus.Pending;
    public DateTime    CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime?   UpdatedAt { get; set; }

    public ApplicationUser User    { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
}

public enum OrderStatus { Pending = 0, Paid = 1, Cancelled = 2 }

// ─── OrderItem ────────────────────────────────────────────────────────────────
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public int SeatId  { get; set; }

    [Column(TypeName = "decimal(10,2)")] public decimal PricePaid { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order  Order  { get; set; } = null!;
    public Seat   Seat   { get; set; } = null!;
    public Ticket? Ticket { get; set; }
}

// ─── Payment ──────────────────────────────────────────────────────────────────
public class Payment
{
    public int Id { get; set; }

    public int     OrderId    { get; set; }
    [MaxLength(100)] public string Provider   { get; set; } = "Simulated";
    [MaxLength(200)] public string ExternalId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")] public decimal Amount { get; set; }

    public PaymentStatus Status    { get; set; } = PaymentStatus.Pending;
    public DateTime?     PaidAt    { get; set; }
    public DateTime      CreatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}

public enum PaymentStatus { Pending = 0, Completed = 1, Failed = 2 }

// ─── Ticket ───────────────────────────────────────────────────────────────────
public class Ticket
{
    public int Id { get; set; }

    public int OrderItemId { get; set; }

    [MaxLength(500)] public string QRCode { get; set; } = string.Empty;

    public bool      IsUsed    { get; set; } = false;
    public DateTime? UsedAt    { get; set; }
    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;

    public OrderItem OrderItem { get; set; } = null!;
    public ICollection<TicketValidation> Validations { get; set; } = new List<TicketValidation>();
}

// ─── TicketValidation ─────────────────────────────────────────────────────────
public class TicketValidation
{
    public int Id { get; set; }

    public int  TicketId { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string DeviceInfo     { get; set; } = string.Empty;
    public bool      WasSuccessful { get; set; }
    public string?   FailureReason { get; set; }

    public Ticket Ticket { get; set; } = null!;
}
