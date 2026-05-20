using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.DTOs;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
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
        HolderEmail    = t.OrderItem.Order.UserId, // email stored via Identity
        EventName      = t.OrderItem.Seat.Showtime.Event.Name,
        VenueName      = t.OrderItem.Seat.Showtime.Event.Venue.Name,
        ShowtimeStart  = t.OrderItem.Seat.Showtime.StartTime,
        SeatLabel      = t.OrderItem.Seat.Label,
        WasAlreadyUsed = alreadyUsed,
        UsedAt         = t.UsedAt
    };
}

// ─── AdminService ─────────────────────────────────────────────────────────────
public class AdminService : IAdminService
{
    private readonly AppDbContext        _db;
    private readonly IConnectionMultiplexer _redis;

    public AdminService(AppDbContext db, IConnectionMultiplexer redis)
    {
        _db    = db;
        _redis = redis;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        const string cacheKey = "admin:dashboard";
        var redisDb = _redis.GetDatabase();

        // Try cache first (5 minutes)
        var cached = await redisDb.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var fromCache = JsonSerializer.Deserialize<DashboardDto>((string)cached!);
            if (fromCache != null) return fromCache;
        }

        var today        = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-30);

        var paidOrders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Where(o => o.Status == OrderStatus.Paid)
            .ToListAsync();

        var todayOrders = paidOrders.Where(o => o.Payment?.PaidAt?.Date == today).ToList();
        var activeEvents = await _db.Events.CountAsync(e => e.IsActive);

        var dailyRevenue = paidOrders
            .Where(o => o.Payment?.PaidAt >= thirtyDaysAgo)
            .GroupBy(o => o.Payment!.PaidAt!.Value.Date)
            .Select(g => new DailyRevenueDto
            {
                Date        = g.Key,
                Revenue     = g.Sum(o => o.Total),
                TicketsSold = g.Sum(o => o.Items.Count)
            })
            .OrderBy(d => d.Date)
            .ToList();

        var totalSeats = await _db.Seats.CountAsync();
        var soldSeats  = await _db.Seats.CountAsync(s => s.Status == SeatStatus.Sold);

        var topEvents = await _db.Events
            .Where(e => e.IsActive)
            .Select(e => new TopEventDto
            {
                EventId   = e.Id,
                EventName = e.Name,
                TicketsSold = e.Showtimes
                    .SelectMany(s => s.Seats)
                    .Count(s => s.Status == SeatStatus.Sold),
                Revenue = e.Showtimes
                    .SelectMany(s => s.Seats)
                    .Where(s => s.Status == SeatStatus.Sold && s.OrderItem != null)
                    .Sum(s => s.OrderItem!.PricePaid),
                OccupancyPct = e.Showtimes.SelectMany(s => s.Seats).Any()
                    ? Math.Round(
                        (double)e.Showtimes.SelectMany(s => s.Seats).Count(s => s.Status == SeatStatus.Sold) /
                        e.Showtimes.SelectMany(s => s.Seats).Count() * 100, 1)
                    : 0
            })
            .OrderByDescending(e => e.Revenue)
            .Take(5)
            .ToListAsync();

        var dashboard = new DashboardDto
        {
            TotalRevenue        = paidOrders.Sum(o => o.Total),
            TotalTicketsSold    = paidOrders.Sum(o => o.Items.Count),
            ActiveEvents        = activeEvents,
            TodayRevenue        = todayOrders.Sum(o => o.Total),
            TodayTicketsSold    = todayOrders.Sum(o => o.Items.Count),
            AverageOccupancyPct = totalSeats > 0 ? Math.Round((double)soldSeats / totalSeats * 100, 1) : 0,
            RevenueByDay        = dailyRevenue,
            TopEvents           = topEvents
        };

        // Cache 5 minutes
        await redisDb.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(dashboard),
            TimeSpan.FromMinutes(5));

        return dashboard;
    }
}
