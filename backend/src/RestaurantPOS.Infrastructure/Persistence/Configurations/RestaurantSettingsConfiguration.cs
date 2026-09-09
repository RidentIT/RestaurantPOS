using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class RestaurantSettingsConfiguration : IEntityTypeConfiguration<RestaurantSettings>
{
    public void Configure(EntityTypeBuilder<RestaurantSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RestaurantSettings");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).IsRequired().HasMaxLength(RestaurantSettings.NameMaxLength);
        builder.Property(s => s.AddressLine1).IsRequired().HasMaxLength(RestaurantSettings.AddressLineMaxLength);
        builder.Property(s => s.AddressLine2).HasMaxLength(RestaurantSettings.AddressLineMaxLength);
        builder.Property(s => s.City).HasMaxLength(RestaurantSettings.CityMaxLength);
        builder.Property(s => s.Phone).HasMaxLength(RestaurantSettings.PhoneMaxLength);
        builder.Property(s => s.LogoPath).HasMaxLength(RestaurantSettings.LogoPathMaxLength);
        builder.Property(s => s.VatRegistrationNumber).HasMaxLength(RestaurantSettings.VatRegistrationNumberMaxLength);
        builder.Property(s => s.TaxRatePercent).IsRequired();
        builder.Property(s => s.ServiceChargeRatePercent).IsRequired();
        builder.Property(s => s.ReceiptFooterMessage).IsRequired().HasMaxLength(RestaurantSettings.ReceiptFooterMaxLength);
        builder.Property(s => s.DefaultPrinterName).HasMaxLength(RestaurantSettings.PrinterNameMaxLength);
        builder.Property(s => s.KitchenPrinterName).HasMaxLength(RestaurantSettings.PrinterNameMaxLength);
        builder.Property(s => s.ApprovalPinMaxAttempts).IsRequired();
        builder.Property(s => s.ApprovalPinLockoutMinutes).IsRequired();
        builder.Property(s => s.BackupFolderPath).HasMaxLength(RestaurantSettings.BackupFolderPathMaxLength);
        builder.Property(s => s.BackupRetentionCount).IsRequired();
    }
}
