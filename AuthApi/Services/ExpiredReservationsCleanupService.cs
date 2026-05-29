using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiGeneral.AuthApi.Services;

public class ExpiredReservationsCleanupService: BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredReservationsCleanupService> _logger;

    public ExpiredReservationsCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredReservationsCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired reservation cleanup service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;

                var expiredSeats = await db.Seats
                    .Where(s =>
                        s.Status == SeatStatus.Reserved &&
                        s.ReservedUntil != null &&
                        s.ReservedUntil <= now)
                    .ToListAsync(stoppingToken);

                if (expiredSeats.Any())
                {
                    foreach (var seat in expiredSeats)
                    {
                        seat.Status = SeatStatus.Available;
                        seat.ReservedUntil = null;
                        seat.ReservedByUserId = null;
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "Released {Count} expired seats.",
                        expiredSeats.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning expired reservations.");
            }

            // revisar cada 30 segundos
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}