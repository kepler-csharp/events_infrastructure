using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.DTOs.EventDTOs;

public class CreateEventRequest
{
    public string    Name            { get; set; } = string.Empty;
    public string    Description     { get; set; } = string.Empty;
    public string?   PosterUrl       { get; set; }
    public int       VenueId         { get; set; }
    public EventType Type            { get; set; }
    public int       DurationMinutes { get; set; }
}