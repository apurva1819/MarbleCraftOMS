using MarbleCraftOMS.Core.Enums;

namespace MarbleCraftOMS.Core.Entities;

public class DistributorOrder
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}
