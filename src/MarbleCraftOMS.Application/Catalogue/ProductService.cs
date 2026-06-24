using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Interfaces;

namespace MarbleCraftOMS.Application.Catalogue;

public class ProductService(
    IProductRepository repo,
    ISupplierRepository supplierRepo,
    IProductBrowseQuery browseQuery,
    ICache<PagedResult<ProductBrowseItem>> cache) : IProductService
{
    public async Task<PagedResult<ProductBrowseItem>> BrowseAsync(int page, int pageSize)
    {
        var cacheKey = $"products:p={page}:ps={pageSize}";
        if (cache.TryGet(cacheKey, out var cached))
            return cached!;

        var result = await browseQuery.BrowseAsync(page, pageSize);
        cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public Task<Product?> GetByIdAsync(int id) => repo.GetByIdAsync(id);

    public async Task<Product> AddAsync(AddProductCommand cmd)
    {
        var supplier = await supplierRepo.GetByIdAsync(cmd.SupplierId);
        if (supplier is null)
            throw new ArgumentException($"Supplier {cmd.SupplierId} not found.", nameof(cmd.SupplierId));

        var product = new Product
        {
            Name = cmd.Name,
            Material = cmd.Material,
            Format = cmd.Format,
            Surface = cmd.Surface,
            Color = cmd.Color,
            Size = cmd.Size,
            CountryOfOrigin = cmd.CountryOfOrigin,
            PricePerUnit = cmd.PricePerUnit,
            SupplierId = cmd.SupplierId
        };
        await repo.AddAsync(product);
        return product;
    }

    public async Task<bool> UpdateAsync(int id, UpdateProductCommand cmd)
    {
        var product = await repo.GetByIdAsync(id);
        if (product is null) return false;

        var supplier = await supplierRepo.GetByIdAsync(cmd.SupplierId);
        if (supplier is null)
            throw new ArgumentException($"Supplier {cmd.SupplierId} not found.", nameof(cmd.SupplierId));

        product.Name = cmd.Name;
        product.Material = cmd.Material;
        product.Format = cmd.Format;
        product.Surface = cmd.Surface;
        product.Color = cmd.Color;
        product.Size = cmd.Size;
        product.CountryOfOrigin = cmd.CountryOfOrigin;
        product.PricePerUnit = cmd.PricePerUnit;
        product.SupplierId = cmd.SupplierId;

        await repo.UpdateAsync(product);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await repo.GetByIdAsync(id);
        if (product is null) return false;
        await repo.DeleteAsync(id);
        return true;
    }
}
