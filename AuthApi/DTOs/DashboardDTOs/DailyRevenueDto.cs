namespace ApiGeneral.AuthApi.DTOs.DashboardDTOs;

public class DailyRevenueDto
{
    public DateTime Date        { get; set; }
    public decimal  Revenue     { get; set; }
    public int      TicketsSold { get; set; }
}