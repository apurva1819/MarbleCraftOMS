using MarbleCraftOMS.Core.Interfaces;

namespace MarbleCraftOMS.Application.Notifications;

public class NotificationService(INotificationRepository repo) : INotificationService
{
    public async Task<List<NotificationResponse>> GetRecentAsync(int? customerId, int count = 20)
    {
        var items = await repo.GetRecentAsync(customerId, count);
        return items.Select(n => new NotificationResponse(
            n.Id, n.Type, n.Title, n.Body, n.IsRead, n.CreatedAt)).ToList();
    }

    public async Task MarkReadAsync(int id)
    {
        await repo.MarkReadAsync(id);
        await repo.SaveAsync();
    }

    public Task MarkAllReadAsync(int? customerId) =>
        repo.MarkAllReadAsync(customerId); // ExecuteUpdateAsync — no SaveAsync needed
}
