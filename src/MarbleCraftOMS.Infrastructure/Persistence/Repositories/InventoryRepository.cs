using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Infrastructure.Persistence.Repositories;

public class InventoryRepository(AppDbContext db) : IInventoryRepository
{
    public async Task<List<StockLot>> GetLotsBySkuAsync(int sku) =>
        await db.StockLots
            .Include(sl => sl.Product)
            .Where(sl => sl.ProductId == sku)
            .ToListAsync();

    public async Task<StockLot?> GetLotAsync(int stockLotId) =>
        await db.StockLots.FindAsync(stockLotId);

    public async Task AddLotAsync(StockLot lot)
    {
        await db.StockLots.AddAsync(lot);
        await db.SaveChangesAsync();
    }

    public async Task SaveAsync() =>
        await db.SaveChangesAsync();
}
