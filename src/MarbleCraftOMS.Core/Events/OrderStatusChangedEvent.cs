namespace MarbleCraftOMS.Core.Events;

public class OrderStatusChangedEvent : IDomainEvent
{
    public int OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required string NewStatus { get; init; }
    public int CustomerId { get; init; }
}
