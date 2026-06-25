using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<DistributorOrder?> GetByIdAsync(int id) =>
        await db.DistributorOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
            .Include(o => o.Lines)
                .ThenInclude(l => l.StockLot)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<List<DistributorOrder>> GetAllAsync() =>
        await db.DistributorOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<List<DistributorOrder>> GetByCustomerIdAsync(int customerId) =>
        await db.DistributorOrders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(DistributorOrder order) =>
        await db.DistributorOrders.AddAsync(order);

    public async Task SaveAsync() =>
        await db.SaveChangesAsync();
}
