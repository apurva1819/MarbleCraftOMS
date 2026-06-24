using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Application.Suppliers;

public interface ISupplierService
{
    Task<List<Supplier>> GetAllAsync();
    Task<Supplier?> GetByIdAsync(int id);
    Task<Supplier> AddAsync(AddSupplierCommand cmd);
    Task<bool> UpdateAsync(int id, UpdateSupplierCommand cmd);
    Task<bool> DeleteAsync(int id);
}
