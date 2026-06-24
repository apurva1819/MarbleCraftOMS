namespace MarbleCraftOMS.Application.Catalogue;

public class ProductBrowseItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Material { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public string Surface { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string Size { get; init; } = string.Empty;
    public string CountryOfOrigin { get; init; } = string.Empty;
    public decimal PricePerUnit { get; init; }
    public int SupplierId { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public int QuantityAvailable { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
