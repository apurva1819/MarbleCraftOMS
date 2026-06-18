using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using MarbleCraftOMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Infrastructure.Persistence.Repositories;

public class SupplierRepository(AppDbContext db) : ISupplierRepository
{
    public async Task<List<Supplier>> GetAllAsync() =>
        await db.Suppliers.ToListAsync();

    public async Task<Supplier?> GetByIdAsync(int id) =>
        await db.Suppliers.FindAsync(id);

    public async Task AddAsync(Supplier supplier)
    {
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        db.Suppliers.Update(supplier);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var supplier = await db.Suppliers.FindAsync(id);
        if (supplier is not null)
        {
            db.Suppliers.Remove(supplier);
            await db.SaveChangesAsync();
        }
    }
}