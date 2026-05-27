using System.Security.Claims;
using ApiGeneral.AuthApi.DTOs.ReceptionistDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.DTOs.TicketDTOs;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGeneral.AuthApi.Controllers;

/// <summary>
/// Módulo de venta asistida en mostrador para tickets.kepler.andrescortes.dev.
/// Solo accesible por Receptionist y Admin.
/// Flujo: lookup/register customer → reserve → checkout → (resend email)
/// </summary>
[ApiController]
[Route("api/receptionist")]
[Authorize(Roles = "Admin,Receptionist")]
public class ReceptionistController : ControllerBase
{
    private readonly IReceptionistService _reception;

    public ReceptionistController(IReceptionistService reception)
        => _reception = reception;

    // ── Customer ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Busca un cliente por email.
    /// Si no existe devuelve 404 → el recepcionista debe registrarlo.
    /// </summary>
    [HttpGet("customers/lookup")]
    public async Task<IActionResult> LookupCustomer([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(ApiResponse<object>.Fail("El parámetro 'email' es obligatorio."));

        var result = await _reception.LookupCustomerAsync(email);

        if (result == null)
            return NotFound(ApiResponse<object>.Fail("No se encontró ningún cliente con ese correo."));

        return Ok(ApiResponse<CustomerLookupDto>.Ok(result));
    }

    /// <summary>
    /// Registra un nuevo cliente desde el mostrador.
    /// Genera contraseña temporal y la envía al correo del cliente.
    /// </summary>
    [HttpPost("customers")]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterCustomerRequest request)
    {
        try
        {
            var result = await _reception.RegisterCustomerAsync(request);
            return Created(
                $"/api/receptionist/customers/lookup?email={request.Email}",
                ApiResponse<CustomerLookupDto>.Ok(
                    result,
                    "Cliente registrado. Se envió la contraseña temporal a su correo."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── Seat Reservation ──────────────────────────────────────────────────────

    /// <summary>
    /// Reserva asientos en nombre de un cliente con TTL extendido de 30 minutos
    /// (en lugar del TTL estándar de 5 min del flujo online).
    /// </summary>
    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve([FromBody] AssistedReserveRequest request)
    {
        var result = await _reception.ReserveForCustomerAsync(request);

        if (!result.Success)
            return Conflict(ApiResponse<AssistedReserveResultDto>.Fail(result.Message));

        return Ok(ApiResponse<AssistedReserveResultDto>.Ok(result, result.Message));
    }

    // ── Checkout ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Completa la venta: crea la orden, registra el pago inmediato (efectivo/terminal)
    /// y genera los tickets QR. Envía el correo con los tickets al cliente.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] AssistedCheckoutRequest request)
    {
        try
        {
            var result = await _reception.CheckoutAsync(request);
            return Ok(ApiResponse<AssistedSaleResultDto>.Ok(
                result,
                $"Venta completada. Se enviaron {result.Tickets.Count} ticket(s) al correo {result.CustomerEmail}."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── Tickets ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Lista los tickets de una orden (para mostrarlos en pantalla al cliente).
    /// Puede consultar cualquier orden sin restricción de usuario.
    /// </summary>
    [HttpGet("orders/{id:int}/tickets")]
    public async Task<IActionResult> GetOrderTickets(int id)
    {
        var result = await _reception.GetOrderTicketsAsync(id);

        if (result == null)
            return NotFound(ApiResponse<object>.Fail("Orden no encontrada."));

        return Ok(ApiResponse<List<OrderTicketDto>>.Ok(result));
    }

    /// <summary>
    /// Reenvía el email con todos los tickets de la orden al correo del cliente.
    /// Útil si el cliente no recibió el correo inicial.
    /// </summary>
    [HttpPost("orders/{id:int}/resend-email")]
    public async Task<IActionResult> ResendEmail(int id)
    {
        try
        {
            var result = await _reception.ResendTicketsEmailAsync(id);

            if (!result.Success)
                return BadRequest(ApiResponse<object>.Fail(
                    "No se pudo enviar el correo. La orden no tiene tickets generados."));

            return Ok(ApiResponse<ResendEmailResultDto>.Ok(
                result,
                $"{result.TicketsSent} ticket(s) reenviados a {result.SentTo}."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
