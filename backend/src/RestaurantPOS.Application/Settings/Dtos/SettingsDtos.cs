namespace RestaurantPOS.Application.Settings.Dtos;

/// <summary>Everything an administrator can configure about the restaurant itself.</summary>
public sealed record RestaurantSettingsDto(
    string Name,
    string AddressLine1,
    string? AddressLine2,
    string? City,
    string? Phone,
    string? LogoPath,
    string? VatRegistrationNumber,
    decimal TaxRatePercent,
    decimal ServiceChargeRatePercent,
    string ReceiptFooterMessage,
    string? DefaultPrinterName,
    string? KitchenPrinterName,
    int ApprovalPinMaxAttempts,
    int ApprovalPinLockoutMinutes,
    string? BackupFolderPath,
    int BackupRetentionCount);
