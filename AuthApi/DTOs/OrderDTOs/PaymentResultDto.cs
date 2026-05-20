namespace ApiGeneral.AuthApi.DTOs.OrderDTOs;

public class PaymentResultDto
{
    public bool      Success       { get; set; }
    public string    TransactionId { get; set; } = string.Empty;
    public decimal   AmountPaid    { get; set; }
    public DateTime  PaidAt        { get; set; }
    public List<TicketSummaryDto> Tickets { get; set; } = new();
}