using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Enums;

namespace MarbleCraftOMS.Infrastructure.Persistence.Configurations;

public class DistributorOrderConfiguration : IEntityTypeConfiguration<DistributorOrder>
{
    public void Configure(EntityTypeBuilder<DistributorOrder> builder)
    {
        builder.ToTable("DistributorOrders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.Notes).HasMaxLength(500);

        builder.HasOne(o => o.Customer)
               .WithMany(c => c.Orders)
               .HasForeignKey(o => o.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new DistributorOrder
            {
                Id = 1,
                CustomerId = 1,
                OrderDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = OrderStatus.Confirmed,
                Notes = "Priority delivery requested.",
                CreatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
