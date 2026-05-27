namespace ApiGeneral.AuthApi.DTOs.TicketDTOs;

public class ScannerHistoryDto
{
    public int      ValidationId  { get; set; }
    public int      TicketId      { get; set; }
    public string   EventName     { get; set; } = string.Empty;
    public string   SeatLabel     { get; set; } = string.Empty;
    public string   DeviceInfo    { get; set; } = string.Empty;
    public bool     WasSuccessful { get; set; }
    public string?  FailureReason { get; set; }
    public DateTime ValidatedAt   { get; set; }
}
