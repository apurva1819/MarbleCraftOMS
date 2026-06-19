using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using MarbleCraftOMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Infrastructure.Persistence.Repositories;


public class ProductRepository(AppDbContext db) : IProductRepository
{
  public async Task<List<Product>> GetAllAsync() =>
    await db.Products
        .Include(p => p.Supplier)
        .ToListAsync();

   public async Task<Product?> GetByIdAsync(int id) =>
    await db.Products
        .Include(p => p.Supplier)
        .FirstOrDefaultAsync(p => p.Id == id);

    public async Task AddAsync(Product product)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        db.Products.Update(product);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is not null)
        {
            db.Products.Remove(product);
            await db.SaveChangesAsync();
        }
    }
}