using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Infrastructure.Persistence.Repositories;

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<List<Notification>> GetRecentAsync(int? customerId, int count) =>
        await db.Notifications
            .Where(n => customerId == null
                ? n.CustomerId == null                       // internal: only broadcasts
                : n.CustomerId == null || n.CustomerId == customerId) // distributor: broadcasts + own
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task AddAsync(Notification notification) =>
        await db.Notifications.AddAsync(notification);

    public async Task MarkReadAsync(int id)
    {
        var n = await db.Notifications.FindAsync(id);
        if (n is not null) n.IsRead = true;
    }

    public async Task MarkAllReadAsync(int? customerId)
    {
        var query = db.Notifications.Where(n => !n.IsRead);
        if (customerId.HasValue)
            query = query.Where(n => n.CustomerId == null || n.CustomerId == customerId);
        await query.ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task SaveAsync() => await db.SaveChangesAsync();
}
