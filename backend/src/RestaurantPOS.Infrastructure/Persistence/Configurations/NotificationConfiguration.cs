using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Type).IsRequired().HasConversion<int>();
        builder.Property(n => n.Severity).IsRequired().HasConversion<int>();
        builder.Property(n => n.Title).IsRequired().HasMaxLength(Notification.TitleMaxLength);
        builder.Property(n => n.Body).HasMaxLength(Notification.BodyMaxLength);
        builder.Property(n => n.DedupeKey).IsRequired().HasMaxLength(Notification.DedupeKeyMaxLength);
        builder.Property(n => n.Link).HasMaxLength(Notification.LinkMaxLength);

        // The bell asks "what is unread for me" constantly, and the dispatcher asks "has this
        // already been said to this person" on every evaluation.
        builder.HasIndex(n => new { n.UserId, n.ReadAtUtc });
        builder.HasIndex(n => new { n.UserId, n.DedupeKey, n.RaisedAtUtc });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(n => n.IsRead);
    }
}

internal sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("NotificationPreferences");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Type).IsRequired().HasConversion<int>();

        // One opinion per person per notification.
        builder.HasIndex(p => new { p.UserId, p.Type }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class NotificationSettingConfiguration : IEntityTypeConfiguration<NotificationSetting>
{
    public void Configure(EntityTypeBuilder<NotificationSetting> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("NotificationSettings");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Type).IsRequired().HasConversion<int>();
        builder.Property(s => s.Threshold).IsRequired();

        // Thresholds are restaurant-wide, so there is exactly one row per notification type.
        builder.HasIndex(s => s.Type).IsUnique();
    }
}
