namespace MarbleCraftOMS.Core.Entities;

public class OrderLine
{
    public int Id { get; set; }

    public int DistributorOrderId { get; set; }
    public DistributorOrder? DistributorOrder { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int StockLotId { get; set; }
    public StockLot? StockLot { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}
