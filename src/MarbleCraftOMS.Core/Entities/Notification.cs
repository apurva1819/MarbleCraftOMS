namespace MarbleCraftOMS.Core.Entities;

public class Notification
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;      // "LowStock" | "OrderStatus"
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int? CustomerId { get; set; }                   // null = visible to internal staff; set = distributor-specific
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
