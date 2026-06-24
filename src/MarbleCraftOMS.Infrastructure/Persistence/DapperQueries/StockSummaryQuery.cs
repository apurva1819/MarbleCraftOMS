using System.Data;
using Dapper;
using MarbleCraftOMS.Application.Inventory;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Infrastructure.Persistence.DapperQueries;

public class StockSummaryQuery(AppDbContext db) : IStockSummaryQuery
{
    private const string Sql = """
        SELECT
            p.Id          AS ProductId,
            p.Name        AS ProductName,
            p.Material,
            COALESCE(SUM(sl.QuantityOnHand), 0)                        AS TotalOnHand,
            COALESCE(SUM(sl.QuantityCommitted), 0)                     AS TotalCommitted,
            COALESCE(SUM(sl.QuantityOnHand - sl.QuantityCommitted), 0) AS TotalAvailable,
            COUNT(sl.Id)                                                AS LotCount
        FROM Products p
        LEFT JOIN StockLots sl ON sl.ProductId = p.Id
        GROUP BY p.Id, p.Name, p.Material
        ORDER BY p.Name;
        """;

    public async Task<List<StockSummaryItem>> GetSummaryAsync()
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        var rows = await conn.QueryAsync<StockSummaryItem>(Sql);
        return rows.ToList();
    }
}
