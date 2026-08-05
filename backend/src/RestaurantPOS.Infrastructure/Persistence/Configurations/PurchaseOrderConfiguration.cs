using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PurchaseOrders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.Notes).HasMaxLength(PurchaseOrder.NotesMaxLength);

        builder.HasIndex(o => new { o.SupplierId, o.Status });

        builder.Metadata
            .FindNavigation(nameof(PurchaseOrder.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // TotalAmount is computed in memory from the lines, not persisted.
        builder.Ignore(o => o.TotalAmount);
    }
}

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PurchaseOrderLines");

        // A raw material cannot appear more than once on the same purchase order.
        builder.HasKey(l => new { l.PurchaseOrderId, l.RawMaterialId });

        builder.Property(l => l.Quantity).IsRequired();
        builder.Property(l => l.UnitPrice).IsRequired();

        builder.Ignore(l => l.LineTotal);
    }
}