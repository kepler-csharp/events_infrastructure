namespace ApiGeneral.AuthApi.DTOs.TicketDTOs;

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