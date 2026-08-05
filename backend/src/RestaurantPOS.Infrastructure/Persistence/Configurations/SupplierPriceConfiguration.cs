using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class SupplierPriceConfiguration : IEntityTypeConfiguration<SupplierPrice>
{
    public void Configure(EntityTypeBuilder<SupplierPrice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SupplierPrices");

        // One current price per supplier, per raw material.
        builder.HasKey(p => new { p.SupplierId, p.RawMaterialId });

        builder.Property(p => p.Price).IsRequired();
        builder.Property(p => p.UpdatedAtUtc).IsRequired();
    }
}

internal sealed class SupplierPriceHistoryEntryConfiguration : IEntityTypeConfiguration<SupplierPriceHistoryEntry>
{
    public void Configure(EntityTypeBuilder<SupplierPriceHistoryEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SupplierPriceHistoryEntries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Price).IsRequired();
        builder.Property(e => e.RecordedAtUtc).IsRequired();

        builder.HasIndex(e => new { e.SupplierId, e.RawMaterialId, e.RecordedAtUtc });
    }
}