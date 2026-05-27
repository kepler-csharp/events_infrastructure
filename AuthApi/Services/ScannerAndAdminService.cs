using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using ApiGeneral.AuthApi.DTOs.DashboardDTOs;
using ApiGeneral.AuthApi.DTOs.TicketDTOs;
using ApiGeneral.AuthApi.Entities.Enums;

namespace ApiGeneral.AuthApi.Services;

// ─── ScannerService ───────────────────────────────────────────────────────────
public class ScannerService : IScannerService
{
    private readonly AppDbContext _db;
    public ScannerService(AppDbContext db) => _db = db;

    public async Task<ValidateTicketResult> ValidateAsync(ValidateTicketRequest request)
    {
        var ticket = await _db.Tickets
            .Include(t => t.OrderItem)
                .ThenInclude(oi => oi.Seat)
                    .ThenInclude(s => s.Showtime)
                        .ThenInclude(st => st.Event)
                            .ThenInclude(e => e.Venue)
            .Include(t => t.OrderItem)
                .ThenInclude(oi => oi.Order)
            .FirstOrDefaultAsync(t => t.QRCode == request.QRCode);

        if (ticket == null)
            return new ValidateTicketResult { IsValid = false, Message = "Ticket not found. Invalid QR code." };

        var validation = new TicketValidation
        {
            TicketId      = ticket.Id,
            DeviceInfo    = request.DeviceInfo,
            WasSuccessful = !ticket.IsUsed,
            FailureReason = ticket.IsUsed ? "Already used" : null
        };
        _db.TicketValidations.Add(validation);

        if (ticket.IsUsed)
        {
            await _db.SaveChangesAsync();
            return new ValidateTicketResult
            {
                IsValid = false,
                Message = "Ticket already used.",
                Ticket  = BuildDetail(ticket, true)
            };
        }

        ticket.IsUsed = true;
        ticket.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ValidateTicketResult
        {
            IsValid = true,
            Message = "Valid ticket. Access granted.",
            Ticket  = BuildDetail(ticket, false)
        };
    }

    private static TicketDetailDto BuildDetail(Ticket t, bool alreadyUsed) => new()
    {
        TicketId       = t.Id,
        HolderEmail    = t.OrderItem.Order.UserId,
        EventName      = t.OrderItem.Seat.Showtime.Event.Name,
        VenueName      = t.OrderItem.Seat.Showtime.Event.Venue.Name,
        ShowtimeStart  = t.OrderItem.Seat.Showtime.StartTime,
        SeatLabel      = t.OrderItem.Seat.Label,
        WasAlreadyUsed = alreadyUsed,
        UsedAt         = t.UsedAt
    };

    public async Task<List<ScannerHistoryDto>> GetTodayHistoryAsync()
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd   = todayStart.AddDays(1);

        return await _db.TicketValidations
            .Include(v => v.Ticket)
                .ThenInclude(t => t.OrderItem)
                    .ThenInclude(oi => oi.Seat)
                        .ThenInclude(s => s.Showtime)
                            .ThenInclude(st => st.Event)
            .Where(v => v.ValidatedAt >= todayStart && v.ValidatedAt < todayEnd)
            .OrderByDescending(v => v.ValidatedAt)
            .Select(v => new ScannerHistoryDto
            {
                ValidationId  = v.Id,
                TicketId      = v.TicketId,
                EventName     = v.Ticket.OrderItem.Seat.Showtime.Event.Name,
                SeatLabel     = v.Ticket.OrderItem.Seat.Label,
                DeviceInfo    = v.DeviceInfo,
                WasSuccessful = v.WasSuccessful,
                FailureReason = v.FailureReason,
                ValidatedAt   = v.ValidatedAt
            })
            .ToListAsync();
    }
}
