using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.DTOs.AdminDTOs;
using ApiGeneral.AuthApi.DTOs.DashboardDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Entities.Enums;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace ApiGeneral.AuthApi.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext                  _db;
    private readonly IConnectionMultiplexer        _redis;
    private readonly UserManager<ApplicationUser>  _users;

    public AdminService(
        AppDbContext                 db,
        IConnectionMultiplexer       redis,
        UserManager<ApplicationUser> users
    )
    {
        _db    = db;
        _redis = redis;
        _users = users;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public async Task<DashboardDto> GetDashboardAsync()
    {
        const string cacheKey = "admin:dashboard";
        var redisDb = _redis.GetDatabase();

        var cached = await redisDb.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var fromCache = JsonSerializer.Deserialize<DashboardDto>((string)cached!);
            if (fromCache != null) return fromCache;
        }

        var today         = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-30);

        var paidOrders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Where(o => o.Status == OrderStatus.Paid)
            .ToListAsync();

        var todayOrders  = paidOrders.Where(o => o.Payment?.PaidAt?.Date == today).ToList();
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
                EventId     = e.Id,
                EventName   = e.Name,
                TicketsSold = e.Showtimes.SelectMany(s => s.Seats).Count(s => s.Status == SeatStatus.Sold),
                Revenue     = e.Showtimes.SelectMany(s => s.Seats)
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

        await redisDb.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(dashboard),
            TimeSpan.FromMinutes(5)
        );

        return dashboard;
    }

    // ── Customers ─────────────────────────────────────────────────────────────

    public async Task<PagedResult<UserAdminDto>> GetCustomersAsync(int page, int pageSize)
    {
        var customerIds = (await _users.GetUsersInRoleAsync("Customer"))
            .Select(u => u.Id)
            .ToHashSet();

        var query = _users.Users.Where(u => customerIds.Contains(u.Id));
        var total = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = new List<UserAdminDto>();
        foreach (var u in users)
            dtos.Add(await ToDto(u));

        return new PagedResult<UserAdminDto>
        {
            Items      = dtos,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<UserAdminDto?> GetUserByIdAsync(string id)
    {
        var user = await _users.FindByIdAsync(id);
        return user == null ? null : await ToDto(user);
    }

    public async Task<UserAdminDto?> UpdateUserAsync(string id, UpdateUserRequest req)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return null;

        user.FullName = req.FullName;
        user.Email    = req.Email;
        user.UserName = req.Email;
        user.IsActive = req.IsActive;

        await _users.UpdateAsync(user);
        return await ToDto(user);
    }

    public async Task<bool> DeactivateUserAsync(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return false;
        user.IsActive = false;
        await _users.UpdateAsync(user);
        return true;
    }

    public async Task<bool> ReactivateUserAsync(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return false;
        user.IsActive = true;
        await _users.UpdateAsync(user);
        return true;
    }

    // ── Employees ─────────────────────────────────────────────────────────────

    private static readonly string[] EmployeeRoles = ["Scanner", "Receptionist"];

    public async Task<PagedResult<UserAdminDto>> GetEmployeesAsync(int page, int pageSize)
    {
        var employeeIds = new HashSet<string>();
        foreach (var role in EmployeeRoles)
        {
            var inRole = await _users.GetUsersInRoleAsync(role);
            foreach (var u in inRole) employeeIds.Add(u.Id);
        }

        var query = _users.Users.Where(u => employeeIds.Contains(u.Id));
        var total = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = new List<UserAdminDto>();
        foreach (var u in users)
            dtos.Add(await ToDto(u));

        return new PagedResult<UserAdminDto>
        {
            Items      = dtos,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<UserAdminDto?> GetEmployeeByIdAsync(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return null;
        var roles = await _users.GetRolesAsync(user);
        if (!roles.Any(r => EmployeeRoles.Contains(r))) return null;
        return await ToDto(user);
    }

    public async Task<UserAdminDto?> UpdateEmployeeAsync(string id, UpdateUserRequest req)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return null;

        user.FullName = req.FullName;
        user.Email    = req.Email;
        user.UserName = req.Email;
        user.IsActive = req.IsActive;

        await _users.UpdateAsync(user);
        return await ToDto(user);
    }

    public async Task<bool> DeactivateEmployeeAsync(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return false;
        user.IsActive = false;
        await _users.UpdateAsync(user);
        return true;
    }

    public async Task<bool> AdminResetPasswordAsync(string id, string newPassword)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return false;

        var token  = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded;
    }

    // ── Reports ───────────────────────────────────────────────────────────────

    public async Task<byte[]> ExportSalesCsvAsync(DateTime from, DateTime to)
    {
        var toEndOfDay = to.Date.AddDays(1).AddTicks(-1);

        var payments = await _db.Payments
            .Include(p => p.Order)
                .ThenInclude(o => o.Items)
                    .ThenInclude(i => i.Seat)
                        .ThenInclude(s => s.Showtime)
                            .ThenInclude(st => st.Event)
            .Where(p => p.PaidAt >= from.Date && p.PaidAt <= toEndOfDay)
            .OrderBy(p => p.PaidAt)
            .ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("OrderId,TransactionId,PaidAt,CustomerUserId,EventName,Seats,TotalAmount");

        foreach (var p in payments)
        {
            var seats = string.Join("|", p.Order.Items.Select(i => i.Seat.Label));
            var eventName = p.Order.Items.FirstOrDefault()?.Seat.Showtime.Event.Name ?? "";
            sb.AppendLine(
                $"{p.OrderId}," +
                $"\"{p.ExternalId}\"," +
                $"{p.PaidAt:yyyy-MM-dd HH:mm:ss}," +
                $"\"{p.Order.UserId}\"," +
                $"\"{eventName.Replace("\"", "\"\"")}\"," +
                $"\"{seats}\"," +
                $"{p.Amount:F2}"
            );
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ── Audit Log ─────────────────────────────────────────────────────────────

    public async Task<PagedResult<AuditLogDto>> GetAuditLogAsync(AuditLogFilterRequest filter)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.AdminEmail))
            query = query.Where(a => a.AdminEmail.Contains(filter.AdminEmail));

        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(a => a.Action.Contains(filter.Action));

        if (filter.From.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(a => a.CreatedAt <= filter.To.Value.AddDays(1).AddTicks(-1));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AuditLogDto
            {
                Id         = a.Id,
                AdminEmail = a.AdminEmail,
                Action     = a.Action,
                EntityType = a.EntityType,
                EntityId   = a.EntityId,
                OldValues  = a.OldValues,
                NewValues  = a.NewValues,
                CreatedAt  = a.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<AuditLogDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = filter.Page,
            PageSize   = filter.PageSize
        };
    }

    public async Task LogAuditAsync(
        string adminUserId, string adminEmail,
        string action, string entityType,
        string? entityId = null,
        string? oldValues = null, string? newValues = null)
    {
        _db.AuditLogs.Add(new Entities.AuditLog
        {
            AdminUserId = adminUserId,
            AdminEmail  = adminEmail,
            Action      = action,
            EntityType  = entityType,
            EntityId    = entityId,
            OldValues   = oldValues,
            NewValues   = newValues
        });
        await _db.SaveChangesAsync();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<UserAdminDto> ToDto(ApplicationUser u)
    {
        var roles = await _users.GetRolesAsync(u);
        return new UserAdminDto
        {
            Id        = u.Id,
            FullName  = u.FullName ?? string.Empty,
            Email     = u.Email    ?? string.Empty,
            PhotoUrl  = u.PhotoUrl,
            IsActive  = u.IsActive,
            CreatedAt = u.CreatedAt,
            Roles     = roles.ToList()
        };
    }
}
