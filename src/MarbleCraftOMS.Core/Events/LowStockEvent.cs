namespace MarbleCraftOMS.Core.Events;

public class LowStockEvent : IDomainEvent
{
    public required string LotNumber { get; init; }
    public required string ProductName { get; init; }
    public int Remaining { get; init; }
    public int Threshold { get; init; }
}
