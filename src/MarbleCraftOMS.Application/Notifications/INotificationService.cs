namespace MarbleCraftOMS.Application.Notifications;

public interface INotificationService
{
    Task<List<NotificationResponse>> GetRecentAsync(int? customerId, int count = 20);
    Task MarkReadAsync(int id);
    Task MarkAllReadAsync(int? customerId);
}
