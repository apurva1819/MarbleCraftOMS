using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.UnitTests.Domain;

public class StockLotTests
{
    [Fact]
    public void CommitStock_ReducesAvailableQuantity()
    {
        var lot = new StockLot { QuantityOnHand = 100, QuantityCommitted = 0 };

        lot.CommitStock(40);

        Assert.Equal(40, lot.QuantityCommitted);
        Assert.Equal(60, lot.QuantityAvailable);
    }

    [Fact]
    public void CommitStock_WhenInsufficientStock_Throws()
    {
        var lot = new StockLot { QuantityOnHand = 50, QuantityCommitted = 45 }; // 5 available

        var ex = Assert.Throws<InvalidOperationException>(() => lot.CommitStock(10));

        Assert.Contains("5", ex.Message); // message should mention the 5 available
    }

    [Fact]
    public void CommitStock_WhenZeroQuantity_ThrowsArgumentException()
    {
        var lot = new StockLot { QuantityOnHand = 100 };

        Assert.Throws<ArgumentException>(() => lot.CommitStock(0));
    }

    [Fact]
    public void CommitStock_WhenNegativeQuantity_ThrowsArgumentException()
    {
        var lot = new StockLot { QuantityOnHand = 100 };

        Assert.Throws<ArgumentException>(() => lot.CommitStock(-1));
    }

    [Fact]
    public void ReleaseStock_RestoresAvailableQuantity()
    {
        var lot = new StockLot { QuantityOnHand = 100, QuantityCommitted = 60 };

        lot.ReleaseStock(60);

        Assert.Equal(0, lot.QuantityCommitted);
        Assert.Equal(100, lot.QuantityAvailable);
    }

    [Fact]
    public void ReleaseStock_WhenMoreThanCommitted_Throws()
    {
        var lot = new StockLot { QuantityOnHand = 100, QuantityCommitted = 30 };

        Assert.Throws<InvalidOperationException>(() => lot.ReleaseStock(50));
    }

    [Fact]
    public void CommitThenRelease_LeavesQuantityUnchanged()
    {
        var lot = new StockLot { QuantityOnHand = 100, QuantityCommitted = 0 };

        lot.CommitStock(40);
        lot.ReleaseStock(40);

        Assert.Equal(0, lot.QuantityCommitted);
        Assert.Equal(100, lot.QuantityAvailable);
    }
}
