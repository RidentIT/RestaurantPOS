using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Status).IsRequired().HasConversion<int>();
        builder.Property(o => o.DiscountType).IsRequired().HasConversion<int>();

        // The daily sequence is only meaningful together with the day it belongs to.
        builder.HasIndex(o => new { o.OrderDate, o.OrderNumber });
        builder.HasIndex(o => new { o.TableId, o.Status });

        builder.HasOne<RestaurantTable>()
            .WithMany()
            .HasForeignKey(o => o.TableId)
            .OnDelete(DeleteBehavior.Restrict);

        foreach (var navigation in new[] { nameof(Order.Items), nameof(Order.Tickets), nameof(Order.Payments) })
        {
            builder.Metadata.FindNavigation(navigation)!.SetPropertyAccessMode(PropertyAccessMode.Field);
        }

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Tickets)
            .WithOne()
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Payments)
            .WithOne()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Receipt)
            .WithOne()
            .HasForeignKey<Receipt>(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Every money figure is derived from the lines and the discount, so none of them are
        // stored: a persisted total is a second source of truth that drifts the moment a line
        // changes without it.
        builder.Ignore(o => o.Subtotal);
        builder.Ignore(o => o.DiscountAmount);
        builder.Ignore(o => o.ServiceChargeAmount);
        builder.Ignore(o => o.TaxAmount);
        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.AmountPaid);
        builder.Ignore(o => o.ChangeDue);
        builder.Ignore(o => o.ActiveItems);
        builder.Ignore(o => o.IsLive);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        // Long enough for the item's own name plus " (" + a size name + ")", e.g.
        // "Chicken Fried Rice (Full)" — the snapshot combines both once a dish has sizes.
        builder.Property(i => i.MenuItemName)
            .IsRequired()
            .HasMaxLength(MenuItem.NameMaxLength + MenuItemVariant.NameMaxLength + 4);

        builder.Property(i => i.UnitPrice).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();

        builder.Property(i => i.SpecialInstructions)
            .HasMaxLength(OrderItem.SpecialInstructionsMaxLength);

        builder.HasIndex(i => i.OrderId);

        builder.HasOne<MenuItemVariant>()
            .WithMany()
            .HasForeignKey(i => i.MenuItemVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(i => i.LineTotal);
    }
}

internal sealed class OrderPaymentConfiguration : IEntityTypeConfiguration<OrderPayment>
{
    public void Configure(EntityTypeBuilder<OrderPayment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OrderPayments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Method).IsRequired().HasConversion<int>();
        builder.Property(p => p.Amount).IsRequired();
        builder.Property(p => p.Reference).HasMaxLength(OrderPayment.ReferenceMaxLength);

        builder.HasIndex(p => p.OrderId);

        builder.Ignore(p => p.ChangeGiven);
    }
}

internal sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Receipts");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Number).IsRequired().HasMaxLength(Receipt.NumberMaxLength);

        builder.HasIndex(r => r.Number).IsUnique();
        builder.HasIndex(r => r.OrderId).IsUnique();
    }
}
