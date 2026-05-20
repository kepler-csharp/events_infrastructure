using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.DTOs.ShowtimesDTOs;

public class ShowtimeDto
{
    public int           Id             { get; set; }
    public int           EventId        { get; set; }
    public string        EventName      { get; set; } = string.Empty;
    public DateTime      StartTime      { get; set; }
    public DateTime      EndTime        { get; set; }
    public decimal       BasePrice      { get; set; }
    public ShowtimeStatus Status        { get; set; }
    public int           AvailableSeats { get; set; }
    public int           TotalSeats     { get; set; }
}