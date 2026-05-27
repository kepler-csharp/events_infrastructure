namespace ApiGeneral.AuthApi.DTOs.EventDTOs;

public class EventStatsDto
{
    public int     EventId         { get; set; }
    public string  EventName       { get; set; } = string.Empty;
    public int     TotalSeats      { get; set; }
    public int     SoldSeats       { get; set; }
    public int     ReservedSeats   { get; set; }
    public int     AvailableSeats  { get; set; }
    public double  OccupancyPct    { get; set; }
    public decimal TotalRevenue    { get; set; }
    public int     TotalOrders     { get; set; }
    public List<ShowtimeStatsDto> Showtimes { get; set; } = new();
}

public class ShowtimeStatsDto
{
    public int     ShowtimeId    { get; set; }
    public DateTime StartTime    { get; set; }
    public int     TotalSeats    { get; set; }
    public int     SoldSeats     { get; set; }
    public double  OccupancyPct  { get; set; }
    public decimal Revenue       { get; set; }
}
