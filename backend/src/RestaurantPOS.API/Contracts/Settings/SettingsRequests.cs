namespace RestaurantPOS.API.Contracts.Settings;

public sealed record UpdateBusinessProfileRequest(
    string Name,
    string AddressLine1,
    string? AddressLine2,
    string? City,
    string? Phone,
    string? LogoPath,
    string? VatRegistrationNumber);

public sealed record UpdateBillChargesRequest(decimal TaxRatePercent, decimal ServiceChargeRatePercent);

public sealed record UpdateReceiptFooterRequest(string Message);

public sealed record UpdateDefaultPrinterRequest(string? PrinterName);

public sealed record UpdateApprovalPinPolicyRequest(int MaxAttempts, int LockoutMinutes);

public sealed record UpdateBackupSettingsRequest(string? BackupFolderPath, int RetentionCount);

public sealed record RestoreBackupRequest(string FileName, string Pin, string ConfirmationText);
