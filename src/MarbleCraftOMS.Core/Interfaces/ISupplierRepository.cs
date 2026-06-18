using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Core.Interfaces;

public interface ISupplierRepository
{
    Task<List<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);// ? means it can return null if no supplier is found with that Id.
    Task AddAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(int id);
}