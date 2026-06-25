using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Core.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetRecentAsync(int? customerId, int count);
    Task AddAsync(Notification notification);
    Task MarkReadAsync(int id);
    Task MarkAllReadAsync(int? customerId);
    Task SaveAsync();
}
