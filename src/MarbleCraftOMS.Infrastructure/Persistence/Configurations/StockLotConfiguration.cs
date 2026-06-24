using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Infrastructure.Persistence.Configurations;

public class StockLotConfiguration : IEntityTypeConfiguration<StockLot>
{
    public void Configure(EntityTypeBuilder<StockLot> builder)
    {
        builder.ToTable("StockLots");
        builder.HasKey(sl => sl.Id);
        builder.Property(sl => sl.LotNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(sl => sl.LotNumber).IsUnique();
        builder.Property(sl => sl.UnitCostPerSqm).HasColumnType("decimal(18,2)");
        builder.Ignore(sl => sl.QuantityAvailable);

        builder.HasOne(sl => sl.Product)
               .WithMany()
               .HasForeignKey(sl => sl.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sl => sl.Supplier)
               .WithMany()
               .HasForeignKey(sl => sl.SupplierId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new StockLot
            {
                Id = 1,
                LotNumber = "LOT-2026-001",
                ProductId = 1,
                SupplierId = 1,
                QuantityOnHand = 50,
                QuantityCommitted = 0,
                UnitCostPerSqm = 195.00m,
                ReceivedDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new StockLot
            {
                Id = 2,
                LotNumber = "LOT-2026-002",
                ProductId = 2,
                SupplierId = 2,
                QuantityOnHand = 200,
                QuantityCommitted = 0,
                UnitCostPerSqm = 28.00m,
                ReceivedDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
