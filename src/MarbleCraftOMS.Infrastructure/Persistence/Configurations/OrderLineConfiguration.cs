using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Infrastructure.Persistence.Configurations;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Ignore(l => l.LineTotal);

        builder.HasOne(l => l.DistributorOrder)
               .WithMany(o => o.Lines)
               .HasForeignKey(l => l.DistributorOrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Product)
               .WithMany()
               .HasForeignKey(l => l.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.StockLot)
               .WithMany()
               .HasForeignKey(l => l.StockLotId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new OrderLine { Id = 1, DistributorOrderId = 1, ProductId = 1, StockLotId = 1, Quantity = 10, UnitPrice = 285.00m },
            new OrderLine { Id = 2, DistributorOrderId = 1, ProductId = 2, StockLotId = 2, Quantity = 30, UnitPrice = 45.00m }
        );
    }
}
