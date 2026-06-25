using MarbleCraftOMS.Application.Inventory;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using NSubstitute;

namespace MarbleCraftOMS.UnitTests.Application;

public class InventoryServiceTests
{
    private readonly IInventoryRepository _repo = Substitute.For<IInventoryRepository>();
    private readonly IStockSummaryQuery _summaryQuery = Substitute.For<IStockSummaryQuery>();
    private readonly InventoryService _svc;

    public InventoryServiceTests()
    {
        _svc = new InventoryService(_repo, _summaryQuery);
    }

    // ── GetBySkuAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBySkuAsync_WhenNoLots_ReturnsNull()
    {
        _repo.GetLotsBySkuAsync(99).Returns(new List<StockLot>());

        var result = await _svc.GetBySkuAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySkuAsync_AggregatesQuantitiesAcrossLots()
    {
        var product = new Product { Id = 1, Name = "White Marble" };
        var lots = new List<StockLot>
        {
            new() { Product = product, QuantityOnHand = 100, QuantityCommitted = 20, LotNumber = "L1" },
            new() { Product = product, QuantityOnHand = 50,  QuantityCommitted = 10, LotNumber = "L2" }
        };
        _repo.GetLotsBySkuAsync(1).Returns(lots);

        var result = await _svc.GetBySkuAsync(1);

        Assert.NotNull(result);
        Assert.Equal("White Marble", result.ProductName);
        Assert.Equal(150, result.TotalOnHand);
        Assert.Equal(30, result.TotalCommitted);
        Assert.Equal(120, result.TotalAvailable);
        Assert.Equal(2, result.Lots.Count);
    }

    // ── AdjustAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AdjustAsync_WhenLotNotFound_ReturnsNullAndDoesNotSave()
    {
        _repo.GetLotAsync(99).Returns((StockLot?)null);

        var result = await _svc.AdjustAsync(new AdjustStockCommand
        {
            StockLotId = 99, Quantity = 10, Type = AdjustmentType.Commit
        });

        Assert.Null(result);
        await _repo.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task AdjustAsync_Commit_IncrementsCommittedAndSaves()
    {
        var lot = new StockLot { LotNumber = "LOT-001", QuantityOnHand = 100, QuantityCommitted = 10 };
        _repo.GetLotAsync(1).Returns(lot);

        var result = await _svc.AdjustAsync(new AdjustStockCommand
        {
            StockLotId = 1, Quantity = 20, Type = AdjustmentType.Commit
        });

        Assert.NotNull(result);
        Assert.Equal(30, result.QuantityCommitted);
        Assert.Equal(70, result.QuantityAvailable);
        await _repo.Received(1).SaveAsync();
    }

    [Fact]
    public async Task AdjustAsync_Release_DecrementsCommittedAndSaves()
    {
        var lot = new StockLot { LotNumber = "LOT-001", QuantityOnHand = 100, QuantityCommitted = 40 };
        _repo.GetLotAsync(1).Returns(lot);

        var result = await _svc.AdjustAsync(new AdjustStockCommand
        {
            StockLotId = 1, Quantity = 15, Type = AdjustmentType.Release
        });

        Assert.NotNull(result);
        Assert.Equal(25, result.QuantityCommitted);
        Assert.Equal(75, result.QuantityAvailable);
        await _repo.Received(1).SaveAsync();
    }
}
