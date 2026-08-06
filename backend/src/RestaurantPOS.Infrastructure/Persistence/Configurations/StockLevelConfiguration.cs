using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("StockLevels");

        // One balance row per raw material, per store.
        builder.HasKey(l => new { l.RawMaterialId, l.Store });

        builder.Property(l => l.Store)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(l => l.QuantityOnHand).IsRequired();
    }
}