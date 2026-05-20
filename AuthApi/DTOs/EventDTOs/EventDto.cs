using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.DTOs.EventDTOs;

public class EventDto
{
    public int       Id              { get; set; }
    public string    Name            { get; set; } = string.Empty;
    public string    Description     { get; set; } = string.Empty;
    public string?   PosterUrl       { get; set; }
    public string    VenueName       { get; set; } = string.Empty;
    public string    VenueCity       { get; set; } = string.Empty;
    public EventType Type            { get; set; }
    public int       DurationMinutes { get; set; }
    public bool      IsActive        { get; set; }
    public DateTime  CreatedAt       { get; set; }
}