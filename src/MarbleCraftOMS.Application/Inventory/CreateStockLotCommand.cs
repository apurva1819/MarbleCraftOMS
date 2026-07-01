namespace MarbleCraftOMS.Application.Inventory;

public class CreateStockLotCommand
{
    public int ProductId { get; set; }
    public int SupplierId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public decimal UnitCostPerSqm { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
}
