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

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm an order with status '{Status}'. Order must be Pending.");
        Status = OrderStatus.Confirmed;
    }

    public void Dispatch()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot dispatch an order with status '{Status}'. Order must be Confirmed first.");
        Status = OrderStatus.Dispatched;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Dispatched)
            throw new InvalidOperationException("Cannot cancel a dispatched order.");
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled.");
        Status = OrderStatus.Cancelled;
    }
}
