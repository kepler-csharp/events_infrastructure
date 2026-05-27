using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.DTOs.EventDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.DTOs.VenueDTOs;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Entities.Enums;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;

namespace ApiGeneral.AuthApi.Services;

// ─── VenueService ─────────────────────────────────────────────────────────────
public class VenueService : IVenueService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    public VenueService(AppDbContext db, IConfiguration confi)
    {
        _db = db;
        _config = confi;
    }

    public async Task<PagedResult<VenueDto>> GetAllAsync(int page, int pageSize)
    {
        var query = _db.Venues.Where(v => v.IsActive);
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(v => v.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => ToDto(v))
            .ToListAsync();

        return new PagedResult<VenueDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<VenueDto?> GetByIdAsync(int id)
    {
        var v = await _db.Venues.FindAsync(id);
        return v == null ? null : ToDto(v);
    }

    public async Task<VenueDto> CreateAsync(CreateVenueRequest r)
    {
        var venue = new Venue { Name = r.Name, Address = r.Address, City = r.City, Capacity = r.Capacity };
        _db.Venues.Add(venue);
        await _db.SaveChangesAsync();
        return ToDto(venue);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var v = await _db.Venues.FindAsync(id);
        if (v == null) return false;
        v.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private static VenueDto ToDto(Venue v) => new()
    {
        Id = v.Id, Name = v.Name, Address = v.Address,
        City = v.City, Capacity = v.Capacity, IsActive = v.IsActive
    };
}

// ─── EventService ─────────────────────────────────────────────────────────────
public class EventService : IEventService
{
    private readonly AppDbContext  _db;
    private readonly IMinioClient  _minio;
    private readonly IConfiguration _config;

    public EventService(AppDbContext db, IMinioClient minio, IConfiguration config)
    {
        _db     = db;
        _minio  = minio;
        _config = config;
    }

    public async Task<PagedResult<EventDto>> GetAllAsync(int page, int pageSize, bool? isActive)
    {
        var query = _db.Events.Include(e => e.Venue).AsQueryable();
        if (isActive.HasValue) query = query.Where(e => e.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => ToDto(e))
            .ToListAsync();

        return new PagedResult<EventDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<EventDto?> GetByIdAsync(int id)
    {
        var e = await _db.Events.Include(x => x.Venue).FirstOrDefaultAsync(x => x.Id == id);
        return e == null ? null : ToDto(e);
    }

    public async Task<EventDto> CreateAsync(CreateEventRequest r)
    {
        _ = await _db.Venues.FindAsync(r.VenueId)
            ?? throw new KeyNotFoundException("Venue not found.");

        var ev = new Event
        {
            Name = r.Name, Description = r.Description, PosterUrl = r.PosterUrl,
            VenueId = r.VenueId, Type = r.Type, DurationMinutes = r.DurationMinutes
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(x => x.Venue).LoadAsync();
        return ToDto(ev);
    }

    public async Task<EventDto?> UpdateAsync(int id, UpdateEventRequest r)
    {
        var ev = await _db.Events.Include(x => x.Venue).FirstOrDefaultAsync(x => x.Id == id);
        if (ev == null) return null;

        ev.Name = r.Name; ev.Description = r.Description; ev.PosterUrl = r.PosterUrl;
        ev.Type = r.Type; ev.DurationMinutes = r.DurationMinutes;
        ev.IsActive = r.IsActive; ev.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(ev);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return false;
        ev.IsActive = false; ev.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<EventDto?> UploadPhotoAsync(int id, IFormFile file)
    {
        var ev = await _db.Events.Include(x => x.Venue).FirstOrDefaultAsync(x => x.Id == id);
        if (ev == null) return null;
        
        const string bucketName = "event-posters";
        
        var exists = 
            await _minio.BucketExistsAsync(
                new BucketExistsArgs().
                    WithBucket(bucketName)
            );
        
        if (!exists)
            await _minio.MakeBucketAsync(
                new MakeBucketArgs()
                    .WithBucket(bucketName)
            );

        var fileName = $"event_{id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        using var stream = file.OpenReadStream();

        await _minio.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(fileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType)
        );

        String? url = _config["Minio:EndpointOut"];
        var photoUrl =
            $"http://{url}/{bucketName}/{fileName}";
        
        ev.UpdatedAt  = DateTime.UtcNow;
        
        var Event = await _db.Events.Include(x => x.Venue)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (Event == null)
        {
            return null;
        }

        Event.PosterUrl = photoUrl;
        
        await _db.SaveChangesAsync();
        return ToDto(ev);
    }

    private static EventDto ToDto(Event e) => new()
    {
        Id = e.Id, Name = e.Name, Description = e.Description, PosterUrl = e.PosterUrl,
        VenueName = e.Venue?.Name ?? "", VenueCity = e.Venue?.City ?? "",
        Type = e.Type, DurationMinutes = e.DurationMinutes,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt
    };

    public async Task<EventStatsDto?> GetStatsAsync(int eventId)
    {
        var ev = await _db.Events
            .Include(e => e.Showtimes)
                .ThenInclude(st => st.Seats)
                    .ThenInclude(s => s.OrderItem)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null) return null;

        var allSeats     = ev.Showtimes.SelectMany(st => st.Seats).ToList();
        var soldSeats    = allSeats.Where(s => s.Status == SeatStatus.Sold).ToList();
        var reservedSeats = allSeats.Where(s => s.Status == SeatStatus.Reserved).ToList();

        var totalRevenue = soldSeats
            .Where(s => s.OrderItem != null)
            .Sum(s => s.OrderItem!.PricePaid);

        var totalOrders = soldSeats
            .Where(s => s.OrderItem != null)
            .Select(s => s.OrderItem!.OrderId)
            .Distinct()
            .Count();

        var showtimeStats = ev.Showtimes.Select(st =>
        {
            var stSeats  = st.Seats.ToList();
            var stSold   = stSeats.Count(s => s.Status == SeatStatus.Sold);
            var stRevenue = stSeats
                .Where(s => s.Status == SeatStatus.Sold && s.OrderItem != null)
                .Sum(s => s.OrderItem!.PricePaid);

            return new ShowtimeStatsDto
            {
                ShowtimeId   = st.Id,
                StartTime    = st.StartTime,
                TotalSeats   = stSeats.Count,
                SoldSeats    = stSold,
                OccupancyPct = stSeats.Count > 0
                    ? Math.Round((double)stSold / stSeats.Count * 100, 1)
                    : 0,
                Revenue = stRevenue
            };
        }).ToList();

        return new EventStatsDto
        {
            EventId        = ev.Id,
            EventName      = ev.Name,
            TotalSeats     = allSeats.Count,
            SoldSeats      = soldSeats.Count,
            ReservedSeats  = reservedSeats.Count,
            AvailableSeats = allSeats.Count - soldSeats.Count - reservedSeats.Count,
            OccupancyPct   = allSeats.Count > 0
                ? Math.Round((double)soldSeats.Count / allSeats.Count * 100, 1)
                : 0,
            TotalRevenue   = totalRevenue,
            TotalOrders    = totalOrders,
            Showtimes      = showtimeStats
        };
    }
}
