using MarbleCraftOMS.Core.Constants;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await EnsureUser(db, "admin",          "Admin@123",  Roles.Admin);
        await EnsureUser(db, "salesagent",     "Sales@123",  Roles.SalesAgent);
        await EnsureUser(db, "distributor",    "Dist@123",   Roles.Distributor, distributorId: 1);
        await EnsureUser(db, "warehousestaff", "Wh@123",     Roles.WarehouseStaff);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureUser(
        AppDbContext db, string username, string password, string role, int? distributorId = null)
    {
        if (await db.Users.AnyAsync(u => u.Username == username)) return;

        db.Users.Add(new AppUser
        {
            Username      = username,
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(password),
            Role          = role,
            DistributorId = distributorId,
            CreatedAt     = DateTime.UtcNow
        });
    }
}
