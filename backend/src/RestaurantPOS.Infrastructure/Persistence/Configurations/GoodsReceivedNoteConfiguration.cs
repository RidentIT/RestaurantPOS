using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class GoodsReceivedNoteConfiguration : IEntityTypeConfiguration<GoodsReceivedNote>
{
    public void Configure(EntityTypeBuilder<GoodsReceivedNote> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("GoodsReceivedNotes");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.ReceivedAtUtc).IsRequired();
        builder.Property(n => n.Notes).HasMaxLength(GoodsReceivedNote.NotesMaxLength);
        builder.Property(n => n.HasIssue).IsRequired();

        builder.HasIndex(n => n.ReceivedAtUtc);
        builder.HasIndex(n => n.PurchaseOrderId);
    }
}