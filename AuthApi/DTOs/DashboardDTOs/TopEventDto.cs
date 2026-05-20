namespace ApiGeneral.AuthApi.DTOs.DashboardDTOs;

public class TopEventDto
{
    public int    EventId        { get; set; }
    public string EventName      { get; set; } = string.Empty;
    public int    TicketsSold    { get; set; }
    public decimal Revenue       { get; set; }
    public double OccupancyPct   { get; set; }
}