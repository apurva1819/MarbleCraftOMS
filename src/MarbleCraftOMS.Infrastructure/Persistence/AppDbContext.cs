using Microsoft.EntityFrameworkCore;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Infrastructure.Persistence;

namespace MarbleCraftOMS.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    
    public DbSet<Supplier> Suppliers { get; set; }
}