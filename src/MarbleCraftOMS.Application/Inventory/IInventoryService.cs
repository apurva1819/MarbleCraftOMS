namespace MarbleCraftOMS.Application.Inventory;

public interface IInventoryService
{
    Task<StockByProductResult?> GetBySkuAsync(int sku);
    Task<List<StockSummaryItem>> GetSummaryAsync();
    Task<AdjustStockResult?> AdjustAsync(AdjustStockCommand cmd);
}
