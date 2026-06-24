namespace MarbleCraftOMS.Application.Inventory;

public class AdjustStockResult
{
    public string LotNumber { get; init; } = string.Empty;
    public int QuantityOnHand { get; init; }
    public int QuantityCommitted { get; init; }
    public int QuantityAvailable { get; init; }
}
