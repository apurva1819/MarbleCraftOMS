using System.Data;
using Dapper;
using MarbleCraftOMS.Application.Catalogue;
using MarbleCraftOMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Infrastructure.Persistence.DapperQueries;

public class ProductBrowseQuery(AppDbContext db) : IProductBrowseQuery
{
    private const string DataSql = """
        SELECT
            p.Id,
            p.Name,
            p.Material,
            p.Format,
            p.Surface,
            p.Color,
            p.Size,
            p.CountryOfOrigin,
            p.PricePerUnit,
            p.SupplierId,
            p.CreatedAt,
            s.CompanyName  AS SupplierName,
            COALESCE(SUM(sl.QuantityOnHand - sl.QuantityCommitted), 0) AS QuantityAvailable
        FROM Products p
        INNER JOIN Suppliers s ON s.Id = p.SupplierId
        LEFT  JOIN StockLots sl ON sl.ProductId = p.Id
        GROUP BY
            p.Id, p.Name, p.Material, p.Format, p.Surface,
            p.Color, p.Size, p.CountryOfOrigin, p.PricePerUnit,
            p.SupplierId, p.CreatedAt, s.CompanyName
        ORDER BY p.Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    private const string CountSql = "SELECT COUNT(*) FROM Products;";

    public async Task<PagedResult<ProductBrowseItem>> BrowseAsync(int page, int pageSize)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        var offset = (page - 1) * pageSize;

        var items = await conn.QueryAsync<ProductBrowseItem>(
            DataSql,
            new { Offset = offset, PageSize = pageSize });

        var totalCount = await conn.ExecuteScalarAsync<int>(CountSql);

        return new PagedResult<ProductBrowseItem>
        {
            Items = items.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
