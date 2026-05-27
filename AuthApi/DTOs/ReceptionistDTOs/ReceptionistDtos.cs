using ApiGeneral.AuthApi.DTOs.TicketDTOs;
using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.DTOs.ReceptionistDTOs;

// ── Customer ──────────────────────────────────────────────────────────────────

public class CustomerLookupDto
{
    public string  UserId    { get; set; } = string.Empty;
    public string  FullName  { get; set; } = string.Empty;
    public string  Email     { get; set; } = string.Empty;
    public string? Phone     { get; set; }
    public string? PhotoUrl  { get; set; }
    public bool    IsActive  { get; set; }
    public bool    IsNew     { get; set; }   // true si se acaba de crear
}

public class RegisterCustomerRequest
{
    public string  FullName { get; set; } = string.Empty;
    public string  Email    { get; set; } = string.Empty;

    /// <summary>Opcional — se guarda en PhoneNumber del IdentityUser.</summary>
    public string? Phone    { get; set; }
}

// ── Seat reservation ──────────────────────────────────────────────────────────

public class AssistedReserveRequest
{
    /// <summary>UserId del cliente (obtenido de lookup/register).</summary>
    public string    CustomerUserId { get; set; } = string.Empty;
    public int       ShowtimeId     { get; set; }
    public List<int> SeatIds        { get; set; } = new();
}

public class AssistedReserveResultDto
{
    public bool      Success         { get; set; }
    public string    Message         { get; set; } = string.Empty;
    public List<int> ReservedSeatIds { get; set; } = new();

    /// <summary>30 minutos de ventana para que el recepcionista complete la venta.</summary>
    public DateTime? ExpiresAt { get; set; }
}

// ── Checkout (crear orden + pago inmediato) ───────────────────────────────────

public class AssistedCheckoutRequest
{
    /// <summary>UserId del cliente al que pertenece la venta.</summary>
    public string    CustomerUserId { get; set; } = string.Empty;

    /// <summary>Asientos ya reservados bajo el CustomerUserId.</summary>
    public List<int> SeatIds        { get; set; } = new();

    /// <summary>Método de pago en mostrador: Cash | Terminal | Transfer.</summary>
    public string    PaymentMethod  { get; set; } = "Cash";
}

public class AssistedSaleResultDto
{
    public bool              Success          { get; set; }
    public int               OrderId          { get; set; }
    public string            TransactionId    { get; set; } = string.Empty;
    public decimal           AmountPaid       { get; set; }
    public DateTime          PaidAt           { get; set; }
    public string            CustomerEmail    { get; set; } = string.Empty;
    public string            CustomerName     { get; set; } = string.Empty;
    public string            PaymentMethod    { get; set; } = string.Empty;
    public List<OrderTicketDto> Tickets       { get; set; } = new();
}

// ── Resend email ──────────────────────────────────────────────────────────────

public class ResendEmailResultDto
{
    public bool   Success      { get; set; }
    public string SentTo       { get; set; } = string.Empty;
    public int    TicketsSent  { get; set; }
}
