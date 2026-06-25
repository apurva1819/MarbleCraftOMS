using MarbleCraftOMS.Core.Events;
using MarbleCraftOMS.Core.Interfaces;
using MarbleCraftOMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarbleCraftOMS.BackgroundServices;

public class LowStockMonitor(
    IEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<LowStockMonitor> logger) : BackgroundService
{
    private const int Threshold = 50;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Give the app a moment to fully start before the first scan
        await Task.Delay(TimeSpan.FromSeconds(30), ct);

        while (!ct.IsCancellationRequested)
        {
            await ScanAsync(ct);
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }

    private async Task ScanAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var lowLots = await db.StockLots
                .Include(l => l.Product)
                .Where(l => l.QuantityOnHand - l.QuantityCommitted <= Threshold)
                .ToListAsync(ct);

            foreach (var lot in lowLots)
            {
                eventBus.Publish(new LowStockEvent
                {
                    LotNumber = lot.LotNumber,
                    ProductName = lot.Product!.Name,
                    Remaining = lot.QuantityAvailable,
                    Threshold = Threshold
                });

                logger.LogWarning("Low stock detected — Lot={LotNumber} Remaining={Remaining}",
                    lot.LotNumber, lot.QuantityAvailable);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LowStockMonitor scan failed");
        }
    }
}
