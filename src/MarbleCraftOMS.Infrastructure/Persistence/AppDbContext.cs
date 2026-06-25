using Microsoft.EntityFrameworkCore;
using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<StockLot> StockLots { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<DistributorOrder> DistributorOrders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<AppUser> Users { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
