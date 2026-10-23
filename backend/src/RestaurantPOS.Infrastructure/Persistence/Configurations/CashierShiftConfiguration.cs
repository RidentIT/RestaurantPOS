using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class CashierShiftConfiguration : IEntityTypeConfiguration<CashierShift>
{
    public void Configure(EntityTypeBuilder<CashierShift> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CashierShifts");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.OpeningCash).IsRequired();
        builder.Property(s => s.Note).HasMaxLength(CashierShift.NoteMaxLength);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.CashierUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.CashierUserId, s.CheckedInAtUtc });

        // A cashier cannot be on two shifts at once. Enforced by the database as well as the
        // handler, so two taps on Check in in the same instant cannot open two.
        builder.HasIndex(s => s.CashierUserId)
            .IsUnique()
            .HasFilter("\"CheckedOutAtUtc\" IS NULL")
            .HasDatabaseName("IX_CashierShifts_OneOpenPerCashier");

        builder.Ignore(s => s.Variance);
        builder.Ignore(s => s.IsOpen);
    }
}
