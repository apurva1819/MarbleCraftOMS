using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Events;
using MarbleCraftOMS.Core.Interfaces;
using MarbleCraftOMS.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarbleCraftOMS.BackgroundServices;

public class NotificationConsumer(
    InMemoryEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var evt in eventBus.Reader.ReadAllAsync(ct))
        {
            try
            {
                Notification? notification = evt switch
                {
                    LowStockEvent e => new Notification
                    {
                        Type = "LowStock",
                        Title = $"Low stock: {e.ProductName}",
                        Body = $"Lot {e.LotNumber} has only {e.Remaining} units available (threshold: {e.Threshold}).",
                        CustomerId = null
                    },
                    OrderStatusChangedEvent e => new Notification
                    {
                        Type = "OrderStatus",
                        Title = $"Order {e.OrderNumber} is now {e.NewStatus}",
                        Body = $"Your order {e.OrderNumber} status has changed to {e.NewStatus}.",
                        CustomerId = e.CustomerId
                    },
                    _ => null
                };

                if (notification is null) continue;

                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                await repo.AddAsync(notification);
                await repo.SaveAsync();

                logger.LogInformation("Notification saved — Type={Type} Title={Title}",
                    notification.Type, notification.Title);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process event {EventType}", evt.GetType().Name);
            }
        }
    }
}
