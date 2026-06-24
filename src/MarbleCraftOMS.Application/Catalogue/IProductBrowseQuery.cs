namespace MarbleCraftOMS.Application.Catalogue;

public interface IProductBrowseQuery
{
    Task<PagedResult<ProductBrowseItem>> BrowseAsync(int page, int pageSize);
}
