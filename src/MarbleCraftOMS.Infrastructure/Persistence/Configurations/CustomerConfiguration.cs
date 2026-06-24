using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CompanyName).IsRequired().HasMaxLength(150);
        builder.Property(c => c.ContactName).HasMaxLength(100);
        builder.Property(c => c.ContactEmail).HasMaxLength(150);
        builder.Property(c => c.ContactPhone).HasMaxLength(20);
        builder.Property(c => c.ShippingAddress).HasMaxLength(250);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.Country).HasMaxLength(100);

        builder.HasData(
            new Customer
            {
                Id = 1,
                CompanyName = "Florentine Interiors",
                ContactName = "Laura Bianchi",
                ContactEmail = "laura@florentineinteriors.com",
                ContactPhone = "+39 055 9876",
                ShippingAddress = "Via dei Servi 12",
                City = "Florence",
                Country = "Italy",
                CreatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Customer
            {
                Id = 2,
                CompanyName = "Mumbai Luxury Homes",
                ContactName = "Arjun Shah",
                ContactEmail = "arjun@mumbaihomes.in",
                ContactPhone = "+91 22 6543210",
                ShippingAddress = "Nariman Point, Block 5",
                City = "Mumbai",
                Country = "India",
                CreatedAt = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
