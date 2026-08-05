using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class StockReleaseConfiguration : IEntityTypeConfiguration<StockRelease>
{
    public void Configure(EntityTypeBuilder<StockRelease> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("StockReleases");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.RequestedAtUtc).IsRequired();
        builder.Property(r => r.ApprovedAtUtc).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(StockRelease.NotesMaxLength);

        builder.HasIndex(r => r.RequestedAtUtc);
    }
}