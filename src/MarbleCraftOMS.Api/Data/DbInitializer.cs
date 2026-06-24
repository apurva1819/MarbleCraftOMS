using MarbleCraftOMS.Core.Constants;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarbleCraftOMS.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        db.Users.AddRange(
            new AppUser
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = Roles.Admin,
                CreatedAt = DateTime.UtcNow
            },
            new AppUser
            {
                Username = "salesagent",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sales@123"),
                Role = Roles.SalesAgent,
                CreatedAt = DateTime.UtcNow
            },
            new AppUser
            {
                Username = "distributor",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dist@123"),
                Role = Roles.Distributor,
                DistributorId = 1,
                CreatedAt = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();
    }
}
