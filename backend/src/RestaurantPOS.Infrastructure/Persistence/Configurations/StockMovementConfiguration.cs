using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("StockMovements");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Store)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.QuantityDelta).IsRequired();

        builder.Property(m => m.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Notes).HasMaxLength(StockMovement.NotesMaxLength);

        // The stock history screen filters by store and raw material and sorts by date, and a
        // GRN/release detail screen looks its lines up by ReferenceId.
        builder.HasIndex(m => new { m.Store, m.RawMaterialId, m.OccurredAtUtc });
        builder.HasIndex(m => m.ReferenceId);
    }
}