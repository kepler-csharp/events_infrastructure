using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.DTOs;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ApiGeneral.AuthApi.Services;

// ─── ShowtimeService ──────────────────────────────────────────────────────────
public class ShowtimeService : IShowtimeService
{
    private readonly AppDbContext _db;
    public ShowtimeService(AppDbContext db) => _db = db;

    public async Task<PagedResult<ShowtimeDto>> GetAllAsync(int page, int pageSize, int? eventId)
    {
        var query = _db.Showtimes.Include(s => s.Event).Include(s => s.Seats).AsQueryable();
        if (eventId.HasValue) query = query.Where(s => s.EventId == eventId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.StartTime)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new PagedResult<ShowtimeDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = total, Page = page, PageSize = pageSize
        };
    }

    public async Task<ShowtimeDto?> GetByIdAsync(int id)
    {
        var s = await _db.Showtimes.Include(x => x.Event).Include(x => x.Seats)
                         .FirstOrDefaultAsync(x => x.Id == id);
        return s == null ? null : ToDto(s);
    }

    public async Task<ShowtimeDto> CreateAsync(CreateShowtimeRequest r)
    {
        var ev = await _db.Events.FindAsync(r.EventId)
            ?? throw new KeyNotFoundException("Event not found.");

        var showtime = new Showtime
        {
            EventId   = r.EventId,
            StartTime = r.StartTime,
            EndTime   = r.StartTime.AddMinutes(ev.DurationMinutes),
            BasePrice = r.BasePrice,
            Status    = ShowtimeStatus.Active
        };

        foreach (var row in r.SeatLayout)
            for (int n = 1; n <= row.SeatCount; n++)
                showtime.Seats.Add(new Seat { Row = row.Row, Number = n, Type = row.Type });

        _db.Showtimes.Add(showtime);
        await _db.SaveChangesAsync();
        await _db.Entry(showtime).Reference(x => x.Event).LoadAsync();
        return ToDto(showtime);
    }

    public async Task<List<SeatDto>> GetSeatsAsync(int showtimeId)
    {
        return await _db.Seats
            .Where(s => s.ShowtimeId == showtimeId)
            .OrderBy(s => s.Row).ThenBy(s => s.Number)
            .Select(s => new SeatDto
            {
                Id = s.Id, Row = s.Row, Number = s.Number,
                Label = s.Row + s.Number.ToString(),
                Type = s.Type, Status = s.Status, ReservedUntil = s.ReservedUntil
            })
            .ToListAsync();
    }

    private static ShowtimeDto ToDto(Showtime s) => new()
    {
        Id = s.Id, EventId = s.EventId, EventName = s.Event?.Name ?? "",
        StartTime = s.StartTime, EndTime = s.EndTime, BasePrice = s.BasePrice,
        Status = s.Status,
        AvailableSeats = s.Seats.Count(x => x.Status == SeatStatus.Available),
        TotalSeats = s.Seats.Count
    };
}

// ─── SeatService (with Redis locks) ──────────────────────────────────────────
public class SeatService : ISeatService
{
    private readonly AppDbContext        _db;
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan ReservationTtl = TimeSpan.FromMinutes(5);

    public SeatService(AppDbContext db, IConnectionMultiplexer redis)
    {
        _db    = db;
        _redis = redis;
    }

    public async Task<ReservationResult> ReserveAsync(string userId, ReserveSeatsRequest request)
    {
        var redisDb    = _redis.GetDatabase();
        var lockKeys   = request.SeatIds.Select(id => $"seat_lock:{id}").ToList();
        var acquiredKeys = new List<string>();

        try
        {
            // Acquire a short Redis lock per seat to prevent race conditions
            foreach (var key in lockKeys)
            {
                var acquired = await redisDb.StringSetAsync(key, userId, TimeSpan.FromSeconds(10), When.NotExists);
                if (!acquired)
                    return new ReservationResult { Success = false, Message = "One or more seats are being reserved. Try again." };
                acquiredKeys.Add(key);
            }

            var seats = await _db.Seats
                .Where(s => request.SeatIds.Contains(s.Id) && s.ShowtimeId == request.ShowtimeId)
                .ToListAsync();

            if (seats.Count != request.SeatIds.Count)
                return new ReservationResult { Success = false, Message = "Some seats were not found." };

            var unavailable = seats.Where(s =>
                s.Status == SeatStatus.Sold ||
                (s.Status == SeatStatus.Reserved && s.ReservedUntil > DateTime.UtcNow && s.ReservedByUserId != userId)
            ).ToList();

            if (unavailable.Any())
                return new ReservationResult { Success = false, Message = $"{unavailable.Count} seat(s) are no longer available." };

            var expiresAt = DateTime.UtcNow.Add(ReservationTtl);
            foreach (var seat in seats)
            {
                seat.Status           = SeatStatus.Reserved;
                seat.ReservedUntil    = expiresAt;
                seat.ReservedByUserId = userId;
            }

            await _db.SaveChangesAsync();

            return new ReservationResult
            {
                Success         = true,
                Message         = "Seats reserved. You have 5 minutes to complete purchase.",
                ReservedSeatIds = seats.Select(s => s.Id).ToList(),
                ExpiresAt       = expiresAt
            };
        }
        finally
        {
            foreach (var key in acquiredKeys)
                await redisDb.KeyDeleteAsync(key);
        }
    }

    public async Task ReleaseAsync(string userId, List<int> seatIds)
    {
        var seats = await _db.Seats
            .Where(s => seatIds.Contains(s.Id) && s.ReservedByUserId == userId && s.Status == SeatStatus.Reserved)
            .ToListAsync();

        foreach (var seat in seats)
        {
            seat.Status           = SeatStatus.Available;
            seat.ReservedUntil    = null;
            seat.ReservedByUserId = null;
        }

        if (seats.Any()) await _db.SaveChangesAsync();
    }
}
