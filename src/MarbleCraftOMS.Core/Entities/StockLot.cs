namespace MarbleCraftOMS.Core.Entities;

public class StockLot
{
    public int Id { get; set; }
    public string LotNumber { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int QuantityOnHand { get; set; }
    public int QuantityCommitted { get; set; }
    public int QuantityAvailable => QuantityOnHand - QuantityCommitted;

    public decimal UnitCostPerSqm { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void CommitStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (quantity > QuantityAvailable)
            throw new InvalidOperationException($"Only {QuantityAvailable} units available in lot {LotNumber}.");

        QuantityCommitted += quantity;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (quantity > QuantityCommitted)
            throw new InvalidOperationException($"Cannot release {quantity} — only {QuantityCommitted} units are committed in lot {LotNumber}.");

        QuantityCommitted -= quantity;
    }

    public void ReceiveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        QuantityOnHand += quantity;
    }

    public void WriteOff(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (quantity > QuantityAvailable)
            throw new InvalidOperationException(
                $"Cannot write off {quantity} — only {QuantityAvailable} uncommitted units available in lot {LotNumber}.");
        QuantityOnHand -= quantity;
    }
}
