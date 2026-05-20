using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.DTOs.SeatDTOs;

public class SeatDto
{
    public int        Id            { get; set; }
    public string     Row           { get; set; } = string.Empty;
    public int        Number        { get; set; }
    public string     Label         { get; set; } = string.Empty;
    public SeatType   Type          { get; set; }
    public SeatStatus Status        { get; set; }
    public DateTime?  ReservedUntil { get; set; }
}