namespace ApiGeneral.AuthApi.DTOs.TicketDTOs;

public class ValidateTicketResult
{
    public bool   IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public TicketDetailDto? Ticket { get; set; }
}