namespace MarbleCraftOMS.Application.Notifications;

public record NotificationResponse(
    int Id,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedAt);
