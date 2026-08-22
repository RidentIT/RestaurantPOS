using System.Net.Http.Json;

namespace RestaurantPOS.IntegrationTests.Common;

public sealed record RestaurantSettingsResponse(
    string Name,
    string AddressLine1,
    string? AddressLine2,
    string? City,
    string? Phone,
    string? LogoPath,
    decimal TaxRatePercent,
    decimal ServiceChargeRatePercent,
    string ReceiptFooterMessage,
    string? DefaultPrinterName,
    int ApprovalPinMaxAttempts,
    int ApprovalPinLockoutMinutes,
    string? BackupFolderPath,
    int BackupRetentionCount);

public sealed record BackupResponse(string FileName, DateTime CreatedAtUtc, long SizeBytes);

public sealed partial class PosApiClient
{
    public Task<HttpResponseMessage> GetRestaurantSettingsAsync() => Http.GetAsync($"{BaseUrl}/settings");

    public Task<HttpResponseMessage> UpdateBusinessProfileAsync(
        string name, string addressLine1, string? addressLine2 = null, string? city = null,
        string? phone = null, string? logoPath = null) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/settings/profile",
            new { name, addressLine1, addressLine2, city, phone, logoPath },
            Json);

    public Task<HttpResponseMessage> UpdateBillChargesAsync(decimal taxRatePercent, decimal serviceChargeRatePercent) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/settings/bill-charges", new { taxRatePercent, serviceChargeRatePercent }, Json);

    public Task<HttpResponseMessage> UpdateReceiptFooterAsync(string message) =>
        Http.PutAsJsonAsync($"{BaseUrl}/settings/receipt-footer", new { message }, Json);

    public Task<HttpResponseMessage> UpdateDefaultPrinterAsync(string? printerName) =>
        Http.PutAsJsonAsync($"{BaseUrl}/settings/printer", new { printerName }, Json);

    public Task<HttpResponseMessage> UpdateApprovalPinPolicyAsync(int maxAttempts, int lockoutMinutes) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/settings/approval-pin-policy", new { maxAttempts, lockoutMinutes }, Json);

    public Task<HttpResponseMessage> UpdateBackupSettingsAsync(string? backupFolderPath, int retentionCount) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/settings/backup", new { backupFolderPath, retentionCount }, Json);

    public Task<HttpResponseMessage> GetBackupsAsync() => Http.GetAsync($"{BaseUrl}/settings/backups");

    public Task<HttpResponseMessage> CreateBackupAsync() => Http.PostAsync($"{BaseUrl}/settings/backups", null);

    public Task<HttpResponseMessage> RestoreBackupAsync(string fileName, string pin, string confirmationText) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/settings/backups/restore", new { fileName, pin, confirmationText }, Json);

    public Task<HttpResponseMessage> RunDailyBackupAsync() =>
        Http.PostAsync($"{BaseUrl}/settings/backups/run-daily", null);
}
