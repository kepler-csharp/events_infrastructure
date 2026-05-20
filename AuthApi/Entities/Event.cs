using System.ComponentModel.DataAnnotations;
using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.Entities;

public class Event
{
    public int Id { get; set; }

    [MaxLength(200)] public string Name        { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [MaxLength(1000)] public string? PosterUrl  { get; set; }

    public int       VenueId          { get; set; }
    public EventType Type             { get; set; } = EventType.Movie;
    public int       DurationMinutes  { get; set; }
    public bool      IsActive         { get; set; } = true;
    public DateTime  CreatedAt        { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt        { get; set; }

    public Venue Venue { get; set; } = null!;
    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}