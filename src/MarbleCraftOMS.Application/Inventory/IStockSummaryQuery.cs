namespace MarbleCraftOMS.Application.Inventory;

public interface IStockSummaryQuery
{
    Task<List<StockSummaryItem>> GetSummaryAsync();
}
