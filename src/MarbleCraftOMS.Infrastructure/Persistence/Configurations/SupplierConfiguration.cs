using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MarbleCraftOMS.Core.Entities;

namespace MarbleCraftOMS.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.CompanyName).IsRequired().HasMaxLength(150);
        builder.Property(s => s.ContactName).HasMaxLength(100);
        builder.Property(s => s.ContactEmail).HasMaxLength(150);
        builder.Property(s => s.ContactPhone).HasMaxLength(20);
        builder.Property(s => s.Address).HasMaxLength(250);
        builder.Property(s => s.Country).HasMaxLength(100);
        builder.Property(s => s.Specialisation).HasMaxLength(200);

        builder.HasData(
            new Supplier
            {
                Id = 1,
                CompanyName = "Carrara Marble Works",
                ContactName = "Marco Rossi",
                ContactEmail = "marco@carrara.it",
                ContactPhone = "+39 0585 12345",
                Address = "Via Carrione 10, Carrara",
                Country = "Italy",
                Specialisation = "White Statuario, Calacatta",
                CreatedAt = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new Supplier
            {
                Id = 2,
                CompanyName = "Rajasthan Stone Exports",
                ContactName = "Priya Mehta",
                ContactEmail = "priya@rajstones.in",
                ContactPhone = "+91 141 456789",
                Address = "Industrial Area, Kishangarh",
                Country = "India",
                Specialisation = "Makrana Marble, Sandstone",
                CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
