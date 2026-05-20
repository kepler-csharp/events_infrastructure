namespace ApiGeneral.AuthApi.DTOs.TicketDTOs;

public class ValidateTicketRequest
{
    public string QRCode     { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
}