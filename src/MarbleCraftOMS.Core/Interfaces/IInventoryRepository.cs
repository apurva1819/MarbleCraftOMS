using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Core.Interfaces;

public interface IInventoryRepository
{
    Task<List<StockLot>> GetLotsBySkuAsync(int sku);
    Task<StockLot?> GetLotAsync(int stockLotId);
    Task SaveAsync();
}
