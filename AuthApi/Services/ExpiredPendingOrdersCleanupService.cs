using ApiGeneral.AuthApi.Data;
using ApiGeneral.AuthApi.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiGeneral.AuthApi.Services;

public class ExpiredPendingOrdersCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredPendingOrdersCleanupService> _logger;
    private readonly IConfiguration _configuration;

    public ExpiredPendingOrdersCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredPendingOrdersCleanupService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired pending orders cleanup service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Tiempo configurable (default: 5 min)
                var expirationMinutes = _configuration.GetValue<int>(
                    "Orders:PendingExpirationMinutes",
                    5);

                var expirationTime = DateTime.UtcNow.AddMinutes(-expirationMinutes);

                var expiredOrders = await db.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Seat)
                    .Where(o =>
                        o.Status == OrderStatus.Pending &&
                        o.CreatedAt <= expirationTime)
                    .ToListAsync(stoppingToken);

                if (expiredOrders.Any())
                {
                    foreach (var order in expiredOrders)
                    {
                        foreach (var item in order.Items)
                        {
                            var seat = item.Seat;

                            // Liberar asiento
                            if (seat.Status == SeatStatus.Reserved)
                            {
                                seat.Status = SeatStatus.Available;
                                seat.ReservedUntil = null;
                                seat.ReservedByUserId = null;
                            }
                        }

                        // Cancelar orden
                        order.Status = OrderStatus.Cancelled;
                        order.UpdatedAt = DateTime.UtcNow;
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "Cancelled {Count} expired pending orders.",
                        expiredOrders.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error cleaning expired pending orders.");
            }

            // Ejecutar cada 30 segundos
            await Task.Delay(
                TimeSpan.FromSeconds(30),
                stoppingToken);
        }
    }
}