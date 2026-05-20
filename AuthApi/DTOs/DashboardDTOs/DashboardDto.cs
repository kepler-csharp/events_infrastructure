namespace ApiGeneral.AuthApi.DTOs.DashboardDTOs;

public class DashboardDto
{
    public decimal TotalRevenue          { get; set; }
    public int     TotalTicketsSold      { get; set; }
    public int     ActiveEvents          { get; set; }
    public decimal TodayRevenue          { get; set; }
    public int     TodayTicketsSold      { get; set; }
    public double  AverageOccupancyPct   { get; set; }
    public List<DailyRevenueDto> RevenueByDay { get; set; } = new();
    public List<TopEventDto>     TopEvents    { get; set; } = new();
}