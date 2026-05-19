using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.DTOs;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiGeneral.AuthApi.Services;

// ─── VenueService ─────────────────────────────────────────────────────────────
public class VenueService : IVenueService
{
    private readonly AppDbContext _db;
    public VenueService(AppDbContext db) => _db = db;

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
    private readonly AppDbContext _db;
    public EventService(AppDbContext db) => _db = db;

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

    private static EventDto ToDto(Event e) => new()
    {
        Id = e.Id, Name = e.Name, Description = e.Description, PosterUrl = e.PosterUrl,
        VenueName = e.Venue?.Name ?? "", VenueCity = e.Venue?.City ?? "",
        Type = e.Type, DurationMinutes = e.DurationMinutes,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt
    };
}
