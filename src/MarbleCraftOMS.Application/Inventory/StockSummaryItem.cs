namespace MarbleCraftOMS.Application.Inventory;

public class StockSummaryItem
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Material { get; init; } = string.Empty;
    public int TotalOnHand { get; init; }
    public int TotalCommitted { get; init; }
    public int TotalAvailable { get; init; }
    public int LotCount { get; init; }
}
