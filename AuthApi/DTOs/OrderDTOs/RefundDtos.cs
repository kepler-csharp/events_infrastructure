using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.DTOs.OrderDTOs;

public class RefundRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public class RefundResultDto
{
    public int          RefundRequestId { get; set; }
    public int          OrderId         { get; set; }
    public RefundStatus Status          { get; set; }
    public string       Reason          { get; set; } = string.Empty;
    public DateTime     RequestedAt     { get; set; }
}
