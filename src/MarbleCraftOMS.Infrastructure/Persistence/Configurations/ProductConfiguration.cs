using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Material).HasMaxLength(100);
        builder.Property(p => p.Format).HasMaxLength(100);
        builder.Property(p => p.Surface).HasMaxLength(100);
        builder.Property(p => p.Color).HasMaxLength(100);
        builder.Property(p => p.Size).HasMaxLength(50);
        builder.Property(p => p.CountryOfOrigin).HasMaxLength(100);
        builder.Property(p => p.PricePerUnit).HasColumnType("decimal(18,2)");

        builder.HasOne(p => p.Supplier)
               .WithMany()
               .HasForeignKey(p => p.SupplierId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Product
            {
                Id = 1,
                Name = "Calacatta Gold Slab",
                Material = "Marble",
                Format = "Slab",
                Surface = "Polished",
                Color = "White/Gold",
                Size = "280x160cm",
                CountryOfOrigin = "Italy",
                PricePerUnit = 285.00m,
                SupplierId = 1,
                CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = 2,
                Name = "Makrana White Tile",
                Material = "Marble",
                Format = "Tile",
                Surface = "Honed",
                Color = "White",
                Size = "60x60cm",
                CountryOfOrigin = "India",
                PricePerUnit = 45.00m,
                SupplierId = 2,
                CreatedAt = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
