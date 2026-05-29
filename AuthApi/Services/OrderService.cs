using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.DTOs.OrderDTOs;
using ApiGeneral.AuthApi.DTOs.Shared;
using ApiGeneral.AuthApi.DTOs.TicketDTOs;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Entities.Enums;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;

namespace ApiGeneral.AuthApi.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext                  _db;
    private readonly UserManager<ApplicationUser>  _users;
    private readonly IEmailService                 _email;
    private readonly IQrService                    _qr;
    private readonly IMinioClient                  _minio;
    private readonly IConfiguration                _config;

    private const string QrBucket = "ticket-qrcodes";

    public OrderService(
        AppDbContext                 db,
        UserManager<ApplicationUser> users,
        IEmailService                email,
        IQrService                   qr,
        IMinioClient                 minio,
        IConfiguration               config
    )
    {
        _db     = db;
        _users  = users;
        _email  = email;
        _qr     = qr;
        _minio  = minio;
        _config = config;
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
            s.ReservedByUserId != userId ||
            s.ReservedUntil < DateTime.UtcNow
        ).ToList();

        if (invalid.Any())
        {
            var details = invalid.Select(s =>
                $"SeatId={s.Id}, " +
                $"Status={s.Status}, " +
                $"ReservedByUserId={s.ReservedByUserId}, " +
                $"ReservedUntil={s.ReservedUntil:O}, " +
                $"CurrentUser={userId}, " +
                $"UtcNow={DateTime.UtcNow:O}"
            );

            throw new InvalidOperationException(
                "Invalid seats: " + string.Join(" | ", details)
            );
        }

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
        
        // Ensure QR bucket exists
        await EnsureBucketAsync(QrBucket);

        var tickets = new List<(OrderItem item, Ticket ticket, string qrBase64)>();

        foreach (var item in order.Items)
        {
            item.Seat.Status           = SeatStatus.Sold;
            item.Seat.ReservedUntil    = null;
            item.Seat.ReservedByUserId = null;

            var qrContent = GenerateQrContent(item.Id);
            var qrPng     = _qr.GenerateQrPng(qrContent);
            var qrBase64  = Convert.ToBase64String(qrPng);

            // Upload QR image to MinIO
            var qrFileName = $"ticket_{item.Id}_{Guid.NewGuid():N}.png";
            var qrImageUrl = await UploadQrToMinioAsync(qrPng, qrFileName);

            var ticket = new Ticket
            {
                OrderItemId = item.Id,
                QRCode      = qrContent,
                QrImageUrl  = qrImageUrl
            };
            _db.Tickets.Add(ticket);
            tickets.Add((item, ticket, qrBase64));
        }

        order.Status    = OrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Send email (fire and forget with error logging; don't fail the payment)
        var user = await _users.FindByIdAsync(userId);
        if (user != null)
        {
            _ = SendTicketEmailsAsync(user, tickets, txId);
        }

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
                QrImageUrl    = t.ticket.QrImageUrl,
                SeatLabel     = t.item.Seat.Label,
                EventName     = t.item.Seat.Showtime.Event.Name,
                ShowtimeStart = t.item.Seat.Showtime.StartTime
            }).ToList()
        };
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<PagedResult<OrderDto>> GetUserOrdersAsync(
        string userId, int page, int pageSize
    )
    {
        var query = _db.Orders.Where(o => o.UserId == userId);
        var total = await query.CountAsync();
        var ids   = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => o.Id)
            .ToListAsync();

        var dtos = new List<OrderDto>();
        foreach (var id in ids)
            dtos.Add(await BuildOrderDtoAsync(id, userId));

        return new PagedResult<OrderDto>
        {
            Items      = dtos,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<OrderDto?> GetByIdAsync(int id, string userId)
    {
        var exists = await _db.Orders.AnyAsync(o => o.Id == id && o.UserId == userId);
        return exists ? await BuildOrderDtoAsync(id, userId) : null;
    }

    public async Task<List<OrderTicketDto>?> GetOrderTicketsAsync(int orderId, string userId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Ticket)
            .Include(o => o.Items)
                .ThenInclude(i => i.Seat)
                    .ThenInclude(s => s.Showtime)
                        .ThenInclude(st => st.Event)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null) return null;

        return order.Items
            .Where(i => i.Ticket != null)
            .Select(i => new OrderTicketDto
            {
                TicketId      = i.Ticket!.Id,
                QRCode        = i.Ticket.QRCode,
                QrImageUrl    = i.Ticket.QrImageUrl,
                SeatLabel     = i.Seat.Label,
                EventName     = i.Seat.Showtime.Event.Name,
                ShowtimeStart = i.Seat.Showtime.StartTime,
                IsUsed        = i.Ticket.IsUsed,
                UsedAt        = i.Ticket.UsedAt
            })
            .ToList();
    }

    public async Task<RefundResultDto> RequestRefundAsync(int orderId, string userId, RefundRequestDto dto)
    {
        var order = await _db.Orders
            .Include(o => o.RefundRequests)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId)
            ?? throw new KeyNotFoundException("Orden no encontrada.");

        if (order.Status != OrderStatus.Paid)
            throw new InvalidOperationException("Solo se pueden solicitar reembolsos de órdenes pagadas.");

        if (order.RefundRequests.Any(r => r.Status == RefundStatus.Pending))
            throw new InvalidOperationException("Ya existe una solicitud de reembolso pendiente para esta orden.");

        var refund = new RefundRequest
        {
            OrderId           = orderId,
            RequestedByUserId = userId,
            Reason            = dto.Reason,
            Status            = RefundStatus.Pending
        };

        _db.RefundRequests.Add(refund);
        await _db.SaveChangesAsync();

        return new RefundResultDto
        {
            RefundRequestId = refund.Id,
            OrderId         = orderId,
            Status          = refund.Status,
            Reason          = refund.Reason,
            RequestedAt     = refund.RequestedAt
        };
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

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
                QRCode        = i.Ticket?.QRCode,
                QrImageUrl    = i.Ticket?.QrImageUrl
            }).ToList()
        };
    }

    private static string GenerateQrContent(int orderItemId)
    {
        var raw = $"TS:{orderItemId}:{DateTime.UtcNow.Ticks}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    private async Task<string?> UploadQrToMinioAsync(byte[] pngBytes, string fileName)
    {
        try
        {
            using var stream = new MemoryStream(pngBytes);

            await _minio.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(QrBucket)
                    .WithObject(fileName)
                    .WithStreamData(stream)
                    .WithObjectSize(pngBytes.Length)
                    .WithContentType("image/png")
            );

            var endpoint = _config["Minio:EndpointOut"];
            return $"http://{endpoint}/{QrBucket}/{fileName}";
        }
        catch
        {
            // No bloquear el pago si falla el upload
            return null;
        }
    }

    private async Task EnsureBucketAsync(string bucketName)
    {
        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName)
        );

        if (!exists)
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName)
            );
        }
    }

    private async Task SendTicketEmailsAsync(
        ApplicationUser user,
        List<(OrderItem item, Ticket ticket, string qrBase64)> tickets,
        string transactionId
    )
    {
        foreach (var (item, ticket, qrBase64) in tickets)
        {
            try
            {
                var data = new TicketEmailData
                {
                    TicketId      = ticket.Id,
                    EventName     = item.Seat.Showtime.Event.Name,
                    VenueName     = item.Seat.Showtime.Event.Venue.Name,
                    VenueAddress  = item.Seat.Showtime.Event.Venue.Address,
                    VenueCity     = item.Seat.Showtime.Event.Venue.City,
                    ShowtimeStart = item.Seat.Showtime.StartTime,
                    SeatLabel     = item.Seat.Label,
                    PricePaid     = item.PricePaid,
                    TransactionId = transactionId,
                    QrCodeBase64  = qrBase64,
                    QrImageUrl    = ticket.QrImageUrl
                };

                await _email.SendTicketEmailAsync(
                    user.Email!,
                    user.FullName ?? user.Email!,
                    data
                );
            }
            catch
            {
                // Silently ignore individual email errors
            }
        }
    }
}
