namespace MarbleCraftOMS.Application.Inventory;

public class StockByProductResult
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int TotalOnHand { get; init; }
    public int TotalCommitted { get; init; }
    public int TotalAvailable { get; init; }
    public List<StockLotDetail> Lots { get; init; } = [];
}

public class StockLotDetail
{
    public int LotId { get; init; }
    public string LotNumber { get; init; } = string.Empty;
    public int OnHand { get; init; }
    public int Committed { get; init; }
    public int Available { get; init; }
    public decimal UnitCostPerSqm { get; init; }
    public DateTime ReceivedDate { get; init; }
}
