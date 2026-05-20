using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.DTOs.ShowtimesDTOs;

public class SeatRowRequest
{
    public string   Row       { get; set; } = string.Empty;
    public int      SeatCount { get; set; }
    public SeatType Type      { get; set; } = SeatType.Standard;
}