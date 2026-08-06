using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("SupplierPayments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Amount).IsRequired();
        builder.Property(p => p.PaymentDateUtc).IsRequired();

        builder.Property(p => p.Method)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.InvoiceReference).HasMaxLength(SupplierPayment.InvoiceReferenceMaxLength);
        builder.Property(p => p.Notes).HasMaxLength(SupplierPayment.NotesMaxLength);

        builder.HasIndex(p => p.PurchaseOrderId);
    }
}