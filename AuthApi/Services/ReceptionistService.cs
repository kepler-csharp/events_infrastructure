using System.Security.Cryptography;
using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.DTOs.ReceptionistDTOs;
using ApiGeneral.AuthApi.DTOs.TicketDTOs;
using ApiGeneral.AuthApi.Entities;
using ApiGeneral.AuthApi.Entities.Enums;
using ApiGeneral.AuthApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using StackExchange.Redis;
using Order = ApiGeneral.AuthApi.Entities.Order;

namespace ApiGeneral.AuthApi.Services;

public class ReceptionistService : IReceptionistService
{
    private readonly AppDbContext                 _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailService                _email;
    private readonly IQrService                   _qr;
    private readonly IMinioClient                 _minio;
    private readonly IConfiguration               _config;
    private readonly IConnectionMultiplexer       _redis;

    private const string QrBucket            = "ticket-qrcodes";
    private static readonly TimeSpan AssistedReservationTtl = TimeSpan.FromMinutes(5);

    private static TimeZoneInfo GetColombiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
    }

    private static DateTime ToColombiaTime(DateTime utcDate)
    {
        var tz = GetColombiaTimeZone();
    
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcDate, DateTimeKind.Utc),
            tz);
    }

    public ReceptionistService(
        AppDbContext                 db,
        UserManager<ApplicationUser> users,
        IEmailService                email,
        IQrService                   qr,
        IMinioClient                 minio,
        IConfiguration               config,
        IConnectionMultiplexer       redis)
    {
        _db     = db;
        _users  = users;
        _email  = email;
        _qr     = qr;
        _minio  = minio;
        _config = config;
        _redis  = redis;
    }

    // ── Customer ──────────────────────────────────────────────────────────────

    public async Task<CustomerLookupDto?> LookupCustomerAsync(string email)
    {
        var user = await _users.FindByEmailAsync(email.Trim().ToLowerInvariant());
        return user == null ? null : ToCustomerDto(user, isNew: false);
    }

    public async Task<CustomerLookupDto> RegisterCustomerAsync(RegisterCustomerRequest request)
    {
        var existing = await _users.FindByEmailAsync(request.Email.Trim());
        if (existing != null)
            throw new InvalidOperationException("Ya existe un cliente con ese correo electrónico.");

        var tempPassword = GenerateTempPassword();

        var user = new ApplicationUser
        {
            FullName    = request.FullName.Trim(),
            Email       = request.Email.Trim(),
            UserName    = request.Email.Trim(),
            PhoneNumber = request.Phone?.Trim(),
            IsActive    = true
        };

        var result = await _users.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));

        await _users.AddToRoleAsync(user, "Customer");

        // Send welcome email with temp credentials (fire & forget)
        _ = _email.SendAssistedRegistrationEmailAsync(
            user.Email!, user.FullName ?? user.Email!, tempPassword);

        return ToCustomerDto(user, isNew: true);
    }

    // ── Seat Reservation ──────────────────────────────────────────────────────

    public async Task<AssistedReserveResultDto> ReserveForCustomerAsync(AssistedReserveRequest request)
    {
        var redisDb  = _redis.GetDatabase();
        var lockKeys = request.SeatIds.Select(id => $"seat_lock:{id}").ToList();
        var acquired = new List<string>();

        try
        {
            foreach (var key in lockKeys)
            {
                var ok = await redisDb.StringSetAsync(
                    key, request.CustomerUserId, TimeSpan.FromSeconds(15), When.NotExists);
                if (!ok)
                    return new AssistedReserveResultDto
                    {
                        Success = false,
                        Message = "Uno o más asientos están siendo reservados por otro proceso. Intenta de nuevo."
                    };
                acquired.Add(key);
            }

            var seats = await _db.Seats
                .Where(s => request.SeatIds.Contains(s.Id) && s.ShowtimeId == request.ShowtimeId)
                .ToListAsync();

            if (seats.Count != request.SeatIds.Count)
                return new AssistedReserveResultDto { Success = false, Message = "Algunos asientos no se encontraron." };

            var unavailable = seats.Where(s =>
                s.Status == SeatStatus.Sold ||
                (s.Status == SeatStatus.Reserved &&
                 s.ReservedUntil > DateTime.UtcNow &&
                 s.ReservedByUserId != request.CustomerUserId)
            ).ToList();

            if (unavailable.Any())
                return new AssistedReserveResultDto
                {
                    Success = false,
                    Message = $"{unavailable.Count} asiento(s) ya no están disponibles."
                };

            // 5-min TTL for assisted sales
            var expiresAtUtc = DateTime.UtcNow.Add(AssistedReservationTtl);

            foreach (var seat in seats)
            {
                seat.Status           = SeatStatus.Reserved;

                // Guardar SIEMPRE en UTC en DB
                seat.ReservedUntil    = expiresAtUtc;

                seat.ReservedByUserId = request.CustomerUserId;
            }

            await _db.SaveChangesAsync();

            return new AssistedReserveResultDto
            {
                Success         = true,
                Message         = "Asientos reservados. Tienes 5 minutos para completar la venta.",
                ReservedSeatIds = seats.Select(s => s.Id).ToList(),

                // SOLO convertir para mostrar al cliente/frontend
                ExpiresAt       = ToColombiaTime(expiresAtUtc)
            };
        }
        finally
        {
            foreach (var key in acquired)
                await redisDb.KeyDeleteAsync(key);
        }
    }

    // ── Checkout ──────────────────────────────────────────────────────────────

    public async Task<AssistedSaleResultDto> CheckoutAsync(AssistedCheckoutRequest request)
    {
        // 1. Validate customer
        var customer = await _users.FindByIdAsync(request.CustomerUserId)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        // 2. Validate seats belong to customer
        var seats = await _db.Seats
            .Include(s => s.Showtime)
                .ThenInclude(st => st.Event)
                    .ThenInclude(e => e.Venue)
            .Where(s => request.SeatIds.Contains(s.Id))
            .ToListAsync();

        if (seats.Count != request.SeatIds.Count)
            throw new InvalidOperationException("Algunos asientos no se encontraron.");

        var invalid = seats.Where(s =>
            s.Status != SeatStatus.Reserved ||
            s.ReservedByUserId != request.CustomerUserId ||
            s.ReservedUntil < DateTime.UtcNow
        ).ToList();

        if (invalid.Any())
            throw new InvalidOperationException(
                "Algunos asientos no están reservados para este cliente o la reserva expiró.");

        // 3. Create order
        var total = seats.Sum(s => s.Showtime.BasePrice);
        var order = new Order
        {
            UserId = request.CustomerUserId,
            Total  = total,
            Status = OrderStatus.Pending,
            Items  = seats.Select(s => new OrderItem
            {
                SeatId    = s.Id,
                PricePaid = s.Showtime.BasePrice
            }).ToList()
        };
        _db.Orders.Add(order);

        // 4. Payment record
        var txId = $"REC-{GenerateShortId()}";
        var payment = new Payment
        {
            Order      = order,
            Provider   = $"Receptionist-{request.PaymentMethod}",
            ExternalId = txId,
            Amount     = total,
            Status     = PaymentStatus.Completed,
            PaidAt     = DateTime.UtcNow
        };
        _db.Payments.Add(payment);

        // 5. Mark seats as sold, generate QR tickets
        await EnsureBucketAsync(QrBucket);
        var ticketResults = new List<(OrderItem item, Ticket ticket, string qrBase64)>();

        foreach (var item in order.Items)
        {
            var seat = seats.First(s => s.Id == item.SeatId);
            seat.Status           = SeatStatus.Sold;
            seat.ReservedUntil    = null;
            seat.ReservedByUserId = null;

            var qrContent  = GenerateQrContent(item);
            var qrPng      = _qr.GenerateQrPng(qrContent);
            var qrBase64   = Convert.ToBase64String(qrPng);
            var qrFileName = $"ticket_{Guid.NewGuid():N}.png";
            var qrImageUrl = await UploadQrAsync(qrPng, qrFileName);

            var ticket = new Ticket
            {
                OrderItem  = item,
                QRCode     = qrContent,
                QrImageUrl = qrImageUrl
            };
            _db.Tickets.Add(ticket);
            ticketResults.Add((item, ticket, qrBase64));
        }

        order.Status    = OrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // 6. Send ticket emails to customer (fire & forget)
        _ = SendAllTicketEmailsAsync(customer, seats, ticketResults, txId);

        // 7. Build result
        var ticketDtos = ticketResults.Select(t =>
        {
            var seat = seats.First(s => s.Id == t.item.SeatId);
            return new OrderTicketDto
            {
                TicketId      = t.ticket.Id,
                QRCode        = t.ticket.QRCode,
                QrImageUrl    = t.ticket.QrImageUrl,
                SeatLabel     = seat.Label,
                EventName     = seat.Showtime.Event.Name,
                ShowtimeStart = seat.Showtime.StartTime,
                IsUsed        = false
            };
        }).ToList();

        return new AssistedSaleResultDto
        {
            Success       = true,
            OrderId       = order.Id,
            TransactionId = txId,
            AmountPaid    = total,
            PaidAt        = payment.PaidAt!.Value,
            CustomerEmail = customer.Email!,
            CustomerName  = customer.FullName ?? customer.Email!,
            PaymentMethod = request.PaymentMethod,
            Tickets       = ticketDtos
        };
    }

    // ── Tickets ───────────────────────────────────────────────────────────────

    public async Task<List<OrderTicketDto>?> GetOrderTicketsAsync(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Ticket)
            .Include(o => o.Items)
                .ThenInclude(i => i.Seat)
                    .ThenInclude(s => s.Showtime)
                        .ThenInclude(st => st.Event)
            .FirstOrDefaultAsync(o => o.Id == orderId);

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

    public async Task<ResendEmailResultDto> ResendTicketsEmailAsync(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Ticket)
            .Include(o => o.Items)
                .ThenInclude(i => i.Seat)
                    .ThenInclude(s => s.Showtime)
                        .ThenInclude(st => st.Event)
                            .ThenInclude(e => e.Venue)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new KeyNotFoundException("Orden no encontrada.");

        if (order.Status != OrderStatus.Paid)
            throw new InvalidOperationException("Solo se pueden reenviar tickets de órdenes pagadas.");

        var customer = await _users.FindByIdAsync(order.UserId)
            ?? throw new KeyNotFoundException("Cliente no encontrado.");

        var txId = order.Payment?.ExternalId ?? "N/A";
        int sent = 0;

        foreach (var item in order.Items.Where(i => i.Ticket != null))
        {
            try
            {
                var qrPng    = _qr.GenerateQrPng(item.Ticket!.QRCode);
                var qrBase64 = Convert.ToBase64String(qrPng);

                await _email.SendTicketEmailAsync(
                    customer.Email!,
                    customer.FullName ?? customer.Email!,
                    new TicketEmailData
                    {
                        TicketId      = item.Ticket.Id,
                        EventName     = item.Seat.Showtime.Event.Name,
                        VenueName     = item.Seat.Showtime.Event.Venue.Name,
                        VenueAddress  = item.Seat.Showtime.Event.Venue.Address,
                        VenueCity     = item.Seat.Showtime.Event.Venue.City,
                        ShowtimeStart = item.Seat.Showtime.StartTime,
                        SeatLabel     = item.Seat.Label,
                        PricePaid     = item.PricePaid,
                        TransactionId = txId,
                        QrCodeBase64  = qrBase64,
                        QrImageUrl    = item.Ticket.QrImageUrl
                    });
                sent++;
            }
            catch { /* No bloquear el reenvío por un ticket individual */ }
        }

        return new ResendEmailResultDto
        {
            Success     = sent > 0,
            SentTo      = customer.Email!,
            TicketsSent = sent
        };
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private static CustomerLookupDto ToCustomerDto(ApplicationUser u, bool isNew) => new()
    {
        UserId   = u.Id,
        FullName = u.FullName ?? string.Empty,
        Email    = u.Email    ?? string.Empty,
        Phone    = u.PhoneNumber,
        PhotoUrl = u.PhotoUrl,
        IsActive = u.IsActive,
        IsNew    = isNew
    };

    private static string GenerateTempPassword()
    {
        // Satisface: mayúscula, minúscula, dígito, mínimo 8 chars
        var guid = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"Tmp@{guid}1a";
    }

    private static string GenerateShortId() =>
        RandomNumberGenerator.GetHexString(8, lowercase: false);

    private static string GenerateQrContent(OrderItem item)
    {
        var raw = $"TS:{item.Id}:{DateTime.UtcNow.Ticks}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    private async Task<string?> UploadQrAsync(byte[] png, string fileName)
    {
        try
        {
            using var stream = new MemoryStream(png);
            await _minio.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(QrBucket)
                    .WithObject(fileName)
                    .WithStreamData(stream)
                    .WithObjectSize(png.Length)
                    .WithContentType("image/png"));
            var endpoint = _config["Minio:EndpointOut"];
            return $"http://{endpoint}/{QrBucket}/{fileName}";
        }
        catch { return null; }
    }

    private async Task EnsureBucketAsync(string bucket)
    {
        var exists = await _minio.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucket));
        if (!exists)
            await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
    }

    private async Task SendAllTicketEmailsAsync(
        ApplicationUser customer,
        List<Seat> seats,
        List<(OrderItem item, Ticket ticket, string qrBase64)> ticketResults,
        string txId)
    {
        foreach (var (item, ticket, qrBase64) in ticketResults)
        {
            try
            {
                var seat = seats.First(s => s.Id == item.SeatId);
                await _email.SendTicketEmailAsync(
                    customer.Email!,
                    customer.FullName ?? customer.Email!,
                    new TicketEmailData
                    {
                        TicketId      = ticket.Id,
                        EventName     = seat.Showtime.Event.Name,
                        VenueName     = seat.Showtime.Event.Venue.Name,
                        VenueAddress  = seat.Showtime.Event.Venue.Address,
                        VenueCity     = seat.Showtime.Event.Venue.City,
                        ShowtimeStart = seat.Showtime.StartTime,
                        SeatLabel     = seat.Label,
                        PricePaid     = item.PricePaid,
                        TransactionId = txId,
                        QrCodeBase64  = qrBase64,
                        QrImageUrl    = ticket.QrImageUrl
                    });
            }
            catch { /* Silently ignore */ }
        }
    }
}
