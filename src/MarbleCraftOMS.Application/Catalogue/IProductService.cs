using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Application.Catalogue;

public interface IProductService
{
    Task<PagedResult<ProductBrowseItem>> BrowseAsync(int page, int pageSize);
    Task<Product?> GetByIdAsync(int id);
    Task<Product> AddAsync(AddProductCommand cmd);
    Task<bool> UpdateAsync(int id, UpdateProductCommand cmd);
    Task<bool> DeleteAsync(int id);
}
