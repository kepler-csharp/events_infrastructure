using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.DTOs.OrderDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Entities.Enums;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApiGeneral.AuthApi.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public OrderService(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db    = db;
        _users = users;
    }

    public async Task<OrderDto> CreateAsync(string userId, CreateOrderRequest request)
    {
        var seats = await _db.Seats
            .Include(s => s.Showtime)
            .Where(s => request.SeatIds.Contains(s.Id))
            .ToListAsync();

        if (seats.Count != request.SeatIds.Count)
            throw new InvalidOperationException("Some seats were not found.");

        var invalid = seats.Where(s =>
            s.Status != SeatStatus.Reserved ||
            s.ReservedByUserId != userId    ||
            s.ReservedUntil < DateTime.UtcNow
        ).ToList();

        if (invalid.Any())
            throw new InvalidOperationException("Some seats are not reserved by you or the reservation expired.");

        var total = seats.Sum(s => s.Showtime.BasePrice);

        var order = new Order
        {
            UserId = userId,
            Total  = total,
            Status = OrderStatus.Pending,
            Items  = seats.Select(s => new OrderItem
            {
                SeatId    = s.Id,
                PricePaid = s.Showtime.BasePrice
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return await BuildOrderDtoAsync(order.Id, userId);
    }

    public async Task<PaymentResultDto> PayAsync(string userId, PayOrderRequest request)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Seat)
                    .ThenInclude(s => s.Showtime)
                        .ThenInclude(st => st.Event)
                            .ThenInclude(e => e.Venue)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Order is not pending.");

        // Simulated payment
        var txId = $"SIM-{Guid.NewGuid():N}".ToUpper()[..16];

        var payment = new Payment
        {
            OrderId    = order.Id,
            Provider   = "Simulated",
            ExternalId = txId,
            Amount     = order.Total,
            Status     = PaymentStatus.Completed,
            PaidAt     = DateTime.UtcNow
        };
        _db.Payments.Add(payment);

        var tickets = new List<(OrderItem item, Ticket ticket)>();
        foreach (var item in order.Items)
        {
            item.Seat.Status           = SeatStatus.Sold;
            item.Seat.ReservedUntil    = null;
            item.Seat.ReservedByUserId = null;

            var ticket = new Ticket
            {
                OrderItemId = item.Id,
                QRCode      = GenerateQR(item.Id)
            };
            _db.Tickets.Add(ticket);
            tickets.Add((item, ticket));
        }

        order.Status    = OrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var user = await _users.FindByIdAsync(userId);

        return new PaymentResultDto
        {
            Success       = true,
            TransactionId = txId,
            AmountPaid    = order.Total,
            PaidAt        = payment.PaidAt!.Value,
            Tickets       = tickets.Select(t => new TicketSummaryDto
            {
                TicketId      = t.ticket.Id,
                QRCode        = t.ticket.QRCode,
                SeatLabel     = t.item.Seat.Label,
                EventName     = t.item.Seat.Showtime.Event.Name,
                ShowtimeStart = t.item.Seat.Showtime.StartTime
            }).ToList()
        };
    }

    public async Task<PagedResult<OrderDto>> GetUserOrdersAsync(string userId, int page, int pageSize)
    {
        var query = _db.Orders.Where(o => o.UserId == userId);
        var total = await query.CountAsync();
        var ids   = await query.OrderByDescending(o => o.CreatedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .Select(o => o.Id).ToListAsync();

        var dtos = new List<OrderDto>();
        foreach (var id in ids)
            dtos.Add(await BuildOrderDtoAsync(id, userId));

        return new PagedResult<OrderDto> { Items = dtos, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<OrderDto?> GetByIdAsync(int id, string userId)
    {
        var exists = await _db.Orders.AnyAsync(o => o.Id == id && o.UserId == userId);
        return exists ? await BuildOrderDtoAsync(id, userId) : null;
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    private async Task<OrderDto> BuildOrderDtoAsync(int orderId, string userId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Seat)
                    .ThenInclude(s => s.Showtime)
                        .ThenInclude(st => st.Event)
            .Include(o => o.Items)
                .ThenInclude(i => i.Ticket)
            .FirstAsync(o => o.Id == orderId);

        var user = await _users.FindByIdAsync(userId);

        return new OrderDto
        {
            Id        = order.Id,
            UserEmail = user?.Email ?? "",
            Total     = order.Total,
            Status    = order.Status,
            CreatedAt = order.CreatedAt,
            Items     = order.Items.Select(i => new OrderItemDto
            {
                Id            = i.Id,
                SeatLabel     = i.Seat.Label,
                EventName     = i.Seat.Showtime.Event.Name,
                ShowtimeStart = i.Seat.Showtime.StartTime,
                PricePaid     = i.PricePaid,
                QRCode        = i.Ticket?.QRCode
            }).ToList()
        };
    }

    private static string GenerateQR(int orderItemId)
    {
        var raw = $"TS:{orderItemId}:{DateTime.UtcNow.Ticks}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }
}
