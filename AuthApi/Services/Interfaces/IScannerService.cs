using ApiGeneral.AuthApi.DTOs;

namespace ApiGeneral.AuthApi.Services.Interfaces;

public interface IScannerService
{
    Task<ValidateTicketResult> ValidateAsync(ValidateTicketRequest request);
}