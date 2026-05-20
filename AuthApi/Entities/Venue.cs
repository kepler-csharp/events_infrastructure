using System.ComponentModel.DataAnnotations;

namespace ApiGeneral.AuthApi.Entities;

public class Venue
{
    public int Id { get; set; }

    [MaxLength(200)] public string Name    { get; set; } = string.Empty;
    [MaxLength(500)] public string Address { get; set; } = string.Empty;
    [MaxLength(100)] public string City    { get; set; } = string.Empty;

    public int  Capacity  { get; set; }
    public bool IsActive  { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}