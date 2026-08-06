using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class KitchenTicketConfiguration : IEntityTypeConfiguration<KitchenTicket>
{
    public void Configure(EntityTypeBuilder<KitchenTicket> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("KitchenTickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Kind).IsRequired().HasConversion<int>();
        builder.Property(t => t.Status).IsRequired().HasConversion<int>();
        builder.Property(t => t.TicketNumber).IsRequired();

        // The kitchen display reads outstanding tickets constantly, so it gets its own index.
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => new { t.OrderId, t.TicketNumber });

        builder.Metadata
            .FindNavigation(nameof(KitchenTicket.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Lines)
            .WithOne()
            .HasForeignKey(l => l.KitchenTicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(t => t.IsWorkable);
    }
}

internal sealed class KitchenTicketLineConfiguration : IEntityTypeConfiguration<KitchenTicketLine>
{
    public void Configure(EntityTypeBuilder<KitchenTicketLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("KitchenTicketLines");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.MenuItemName).IsRequired().HasMaxLength(MenuItem.NameMaxLength);
        builder.Property(l => l.Quantity).IsRequired();

        builder.Property(l => l.SpecialInstructions)
            .HasMaxLength(OrderItem.SpecialInstructionsMaxLength);

        builder.Property(l => l.Note).HasMaxLength(KitchenTicketLine.NoteMaxLength);

        builder.HasIndex(l => l.KitchenTicketId);
    }
}
