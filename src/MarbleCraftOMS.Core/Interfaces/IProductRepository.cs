using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Core.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);// ? means it can return null if no product is found with that Id.
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
}