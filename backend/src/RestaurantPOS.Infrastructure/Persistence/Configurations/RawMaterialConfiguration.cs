using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class RawMaterialConfiguration : IEntityTypeConfiguration<RawMaterial>
{
    public void Configure(EntityTypeBuilder<RawMaterial> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RawMaterials");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(RawMaterial.NameMaxLength);

        builder.HasIndex(r => r.Name).IsUnique();

        builder.Property(r => r.UnitOfMeasurement)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.IsActive).IsRequired();
    }
}