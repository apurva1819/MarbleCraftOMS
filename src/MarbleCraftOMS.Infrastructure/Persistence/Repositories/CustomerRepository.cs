using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Infrastructure.Persistence.Repositories;

public class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public async Task<List<Customer>> GetAllAsync() =>
        await db.Customers.ToListAsync();

    public async Task<Customer?> GetByIdAsync(int id) =>
        await db.Customers.FindAsync(id);

    public async Task AddAsync(Customer customer)
    {
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Customer customer)
    {
        db.Customers.Update(customer);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await db.Customers.FindAsync(id);
        if (customer is not null)
        {
            db.Customers.Remove(customer);
            await db.SaveChangesAsync();
        }
    }
}
