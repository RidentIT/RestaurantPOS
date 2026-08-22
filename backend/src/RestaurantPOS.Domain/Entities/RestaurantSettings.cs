using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// The restaurant's own configuration: who it is on a receipt, what it charges on top of the
/// bill, how an approval PIN behaves, and where its backups live.
/// </summary>
/// <remarks>
/// Exactly one row ever exists — there is one restaurant. A single settings row is simpler than a
/// table of key-value pairs for a fixed, known set of fields, and keeps every value strongly
/// typed rather than parsed out of a string. The database seeder creates the row once on first
/// run, the same way it guarantees an administrator exists; nothing here is ever created twice.
/// </remarks>
public sealed class RestaurantSettings : BaseEntity
{
    public const int NameMaxLength = 150;
    public const int AddressLineMaxLength = 200;
    public const int CityMaxLength = 100;
    public const int PhoneMaxLength = 30;
    public const int LogoPathMaxLength = 400;
    public const int ReceiptFooterMaxLength = 200;
    public const int PrinterNameMaxLength = 200;
    public const int BackupFolderPathMaxLength = 400;

    // EF Core materialisation.
    private RestaurantSettings()
    {
    }

    private RestaurantSettings(string name, string addressLine1, string? city, string? phone)
    {
        Name = NormaliseRequired(name, NameMaxLength, nameof(name));
        AddressLine1 = NormaliseRequired(addressLine1, AddressLineMaxLength, nameof(addressLine1));
        City = NormaliseOptional(city, CityMaxLength);
        Phone = NormaliseOptional(phone, PhoneMaxLength);

        ReceiptFooterMessage = "Thank You! Come Again!";
        ApprovalPinMaxAttempts = 3;
        ApprovalPinLockoutMinutes = 5;
        BackupRetentionCount = 7;
    }

    // ----- Business profile (printed on every receipt and KOT) -----

    public string Name { get; private set; } = string.Empty;

    public string AddressLine1 { get; private set; } = string.Empty;

    public string? AddressLine2 { get; private set; }

    public string? City { get; private set; }

    public string? Phone { get; private set; }

    /// <summary>Path to the logo file, relative to the attachments root. Null shows no logo.</summary>
    public string? LogoPath { get; private set; }

    // ----- Bill charges -----

    /// <summary>
    /// Percentage added on top of the discounted subtotal, 0-100. Zero by default — the owner
    /// prices VAT into each dish rather than itemising it, but wants the option available without
    /// a rebuild if that changes.
    /// </summary>
    public decimal TaxRatePercent { get; private set; }

    /// <summary>Percentage added on top of the discounted subtotal, 0-100. Zero by default.</summary>
    public decimal ServiceChargeRatePercent { get; private set; }

    // ----- Receipt -----

    public string ReceiptFooterMessage { get; private set; } = string.Empty;

    // ----- Printing -----

    /// <summary>The printer bills and KOTs go to. Null uses the system default.</summary>
    public string? DefaultPrinterName { get; private set; }

    // ----- Security -----

    /// <summary>Wrong PIN attempts allowed before a terminal is paused.</summary>
    public int ApprovalPinMaxAttempts { get; private set; }

    public int ApprovalPinLockoutMinutes { get; private set; }

    // ----- Backup -----

    /// <summary>Where backup archives are written. Null uses the default Backups folder.</summary>
    public string? BackupFolderPath { get; private set; }

    /// <summary>How many backups to keep before the oldest are pruned.</summary>
    public int BackupRetentionCount { get; private set; }

    public static RestaurantSettings Create(string name, string addressLine1, string? city, string? phone) =>
        new(name, addressLine1, city, phone);

    public void UpdateProfile(
        string name, string addressLine1, string? addressLine2, string? city, string? phone, string? logoPath)
    {
        Name = NormaliseRequired(name, NameMaxLength, nameof(name));
        AddressLine1 = NormaliseRequired(addressLine1, AddressLineMaxLength, nameof(addressLine1));
        AddressLine2 = NormaliseOptional(addressLine2, AddressLineMaxLength);
        City = NormaliseOptional(city, CityMaxLength);
        Phone = NormaliseOptional(phone, PhoneMaxLength);
        LogoPath = NormaliseOptional(logoPath, LogoPathMaxLength);
    }

    public void UpdateBillCharges(decimal taxRatePercent, decimal serviceChargeRatePercent)
    {
        TaxRatePercent = ValidatePercentage(taxRatePercent, nameof(taxRatePercent));
        ServiceChargeRatePercent = ValidatePercentage(serviceChargeRatePercent, nameof(serviceChargeRatePercent));
    }

    public void UpdateReceiptFooter(string message) =>
        ReceiptFooterMessage = NormaliseRequired(message, ReceiptFooterMaxLength, nameof(message));

    public void UpdateDefaultPrinter(string? printerName) =>
        DefaultPrinterName = NormaliseOptional(printerName, PrinterNameMaxLength);

    public void UpdateApprovalPinPolicy(int maxAttempts, int lockoutMinutes)
    {
        ApprovalPinMaxAttempts = maxAttempts is >= 1 and <= 10
            ? maxAttempts
            : throw new ArgumentOutOfRangeException(
                nameof(maxAttempts), maxAttempts, "Attempts allowed must be between 1 and 10.");

        ApprovalPinLockoutMinutes = lockoutMinutes is >= 1 and <= 60
            ? lockoutMinutes
            : throw new ArgumentOutOfRangeException(
                nameof(lockoutMinutes), lockoutMinutes, "The lockout must be between 1 and 60 minutes.");
    }

    public void UpdateBackupSettings(string? backupFolderPath, int retentionCount)
    {
        BackupFolderPath = NormaliseOptional(backupFolderPath, BackupFolderPathMaxLength);

        BackupRetentionCount = retentionCount is >= 1 and <= 60
            ? retentionCount
            : throw new ArgumentOutOfRangeException(
                nameof(retentionCount), retentionCount, "Retention must be between 1 and 60 backups.");
    }

    private static decimal ValidatePercentage(decimal value, string paramName) =>
        value is >= 0 and <= 100
            ? value
            : throw new ArgumentOutOfRangeException(paramName, value, "A percentage must be between 0 and 100.");

    private static string NormaliseRequired(string value, int maxLength, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        var trimmed = value.Trim();

        return trimmed.Length > maxLength
            ? throw new ArgumentException($"Cannot exceed {maxLength} characters.", paramName)
            : trimmed;
    }

    private static string? NormaliseOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length > maxLength
            ? throw new ArgumentException($"Cannot exceed {maxLength} characters.")
            : trimmed;
    }
}
