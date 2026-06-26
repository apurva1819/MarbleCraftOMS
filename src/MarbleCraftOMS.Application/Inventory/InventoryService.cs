using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;

namespace MarbleCraftOMS.Application.Inventory;

public class InventoryService(
    IInventoryRepository repo,
    IStockSummaryQuery summaryQuery) : IInventoryService
{
    public async Task<StockByProductResult?> GetBySkuAsync(int sku)
    {
        var lots = await repo.GetLotsBySkuAsync(sku);
        if (lots.Count == 0) return null;

        return new StockByProductResult
        {
            ProductId = sku,
            ProductName = lots[0].Product!.Name,
            TotalOnHand = lots.Sum(l => l.QuantityOnHand),
            TotalCommitted = lots.Sum(l => l.QuantityCommitted),
            TotalAvailable = lots.Sum(l => l.QuantityAvailable),
            Lots = lots.Select(l => new StockLotDetail
            {
                LotId = l.Id,
                LotNumber = l.LotNumber,
                OnHand = l.QuantityOnHand,
                Committed = l.QuantityCommitted,
                Available = l.QuantityAvailable,
                UnitCostPerSqm = l.UnitCostPerSqm,
                ReceivedDate = l.ReceivedDate
            }).ToList()
        };
    }

    public Task<List<StockSummaryItem>> GetSummaryAsync() => summaryQuery.GetSummaryAsync();

    public async Task<StockLotDetail> CreateLotAsync(CreateStockLotCommand cmd)
    {
        if (cmd.QuantityOnHand <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(cmd.QuantityOnHand));

        var lot = new StockLot
        {
            ProductId      = cmd.ProductId,
            SupplierId     = cmd.SupplierId,
            LotNumber      = cmd.LotNumber,
            QuantityOnHand = cmd.QuantityOnHand,
            UnitCostPerSqm = cmd.UnitCostPerSqm,
            ReceivedDate   = cmd.ReceivedDate
        };

        await repo.AddLotAsync(lot);

        return new StockLotDetail
        {
            LotId          = lot.Id,
            LotNumber      = lot.LotNumber,
            OnHand         = lot.QuantityOnHand,
            Committed      = lot.QuantityCommitted,
            Available      = lot.QuantityAvailable,
            UnitCostPerSqm = lot.UnitCostPerSqm,
            ReceivedDate   = lot.ReceivedDate
        };
    }

    public async Task<AdjustStockResult?> AdjustAsync(AdjustStockCommand cmd)
    {
        var lot = await repo.GetLotAsync(cmd.StockLotId);
        if (lot is null) return null;

        if (cmd.Type == AdjustmentType.Commit)
            lot.CommitStock(cmd.Quantity);
        else
            lot.ReleaseStock(cmd.Quantity);

        await repo.SaveAsync();

        return new AdjustStockResult
        {
            LotNumber = lot.LotNumber,
            QuantityOnHand = lot.QuantityOnHand,
            QuantityCommitted = lot.QuantityCommitted,
            QuantityAvailable = lot.QuantityAvailable
        };
    }
}
