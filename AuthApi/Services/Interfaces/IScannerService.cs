using ApiGeneral.AuthApi.DTOs.TicketDTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IScannerService
{
    Task<ValidateTicketResult> ValidateAsync(ValidateTicketRequest request);
}