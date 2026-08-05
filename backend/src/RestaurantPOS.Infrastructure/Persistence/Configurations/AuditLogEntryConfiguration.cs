using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AuditLogEntries");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(AuditLogEntry.EntityTypeMaxLength);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(AuditLogEntry.ActionMaxLength);

        builder.Property(a => a.PerformedByName)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(a => a.Summary)
            .IsRequired()
            .HasMaxLength(AuditLogEntry.SummaryMaxLength);

        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}