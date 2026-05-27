using ApiGeneral.AuthApi.DTOs.ReceptionistDTOs;
using ApiGeneral.AuthApi.DTOs.TicketDTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IReceptionistService
{
    /// <summary>
    /// Busca un cliente por email.
    /// Devuelve null si no existe.
    /// </summary>
    Task<CustomerLookupDto?> LookupCustomerAsync(string email);

    /// <summary>
    /// Registra un nuevo cliente. Genera contraseña temporal y la envía por correo.
    /// Lanza InvalidOperationException si el email ya está registrado.
    /// </summary>
    Task<CustomerLookupDto> RegisterCustomerAsync(RegisterCustomerRequest request);

    /// <summary>
    /// Reserva asientos para un cliente con TTL extendido de 30 minutos
    /// (en lugar del TTL estándar de 5 min del flujo online).
    /// </summary>
    Task<AssistedReserveResultDto> ReserveForCustomerAsync(AssistedReserveRequest request);

    /// <summary>
    /// Crea la orden y procesa el pago inmediatamente (venta presencial).
    /// Genera los tickets y envía el email al cliente.
    /// </summary>
    Task<AssistedSaleResultDto> CheckoutAsync(AssistedCheckoutRequest request);

    /// <summary>Devuelve los tickets de una orden (para mostrar en pantalla).</summary>
    Task<List<OrderTicketDto>?> GetOrderTicketsAsync(int orderId);

    /// <summary>Reenvía el email de tickets al correo del cliente.</summary>
    Task<ResendEmailResultDto> ResendTicketsEmailAsync(int orderId);
}
