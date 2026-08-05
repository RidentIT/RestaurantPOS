using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Suppliers");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(Supplier.NameMaxLength);

        builder.HasIndex(s => s.Name).IsUnique();

        builder.Property(s => s.ContactName).HasMaxLength(Supplier.ContactNameMaxLength);
        builder.Property(s => s.Phone).HasMaxLength(Supplier.PhoneMaxLength);
        builder.Property(s => s.Email).HasMaxLength(Supplier.EmailMaxLength);
        builder.Property(s => s.Address).HasMaxLength(Supplier.AddressMaxLength);
        builder.Property(s => s.PaymentTermsDays).IsRequired();

        builder.Property(s => s.IsActive).IsRequired();
    }
}