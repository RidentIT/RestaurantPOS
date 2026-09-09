using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Mappings;

public static class SettingsMappings
{
    public static RestaurantSettingsDto ToDto(this RestaurantSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new RestaurantSettingsDto(
            settings.Name,
            settings.AddressLine1,
            settings.AddressLine2,
            settings.City,
            settings.Phone,
            settings.LogoPath,
            settings.VatRegistrationNumber,
            settings.TaxRatePercent,
            settings.ServiceChargeRatePercent,
            settings.ReceiptFooterMessage,
            settings.DefaultPrinterName,
            settings.KitchenPrinterName,
            settings.ApprovalPinMaxAttempts,
            settings.ApprovalPinLockoutMinutes,
            settings.BackupFolderPath,
            settings.BackupRetentionCount);
    }
}
