namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>One archive on disk: the database plus the receipt-attachment images, zipped together.</summary>
public sealed record BackupFileInfo(string FileName, DateTime CreatedAtUtc, long SizeBytes);

/// <summary>
/// Creates, lists and restores backup archives.
/// </summary>
/// <remarks>
/// A null <c>folderPath</c> everywhere here means "the administrator has not chosen a folder yet",
/// which resolves to a default <c>Backups</c> folder beside the app rather than failing — a fresh
/// install can back itself up before anyone has ever opened Settings.
/// </remarks>
public interface IBackupService
{
    Task<IReadOnlyCollection<BackupFileInfo>> ListAsync(string? folderPath, CancellationToken cancellationToken);

    /// <summary>
    /// Takes a live, consistent copy of the database (SQLite's <c>VACUUM INTO</c>, so the till
    /// keeps serving requests while it runs) together with the attachments folder, then prunes the
    /// oldest archives beyond <paramref name="retentionCount"/>.
    /// </summary>
    Task<BackupFileInfo> CreateAsync(string? folderPath, int retentionCount, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the live database and attachments with the contents of one archive. The caller is
    /// responsible for shutting the application down immediately afterwards — every connection this
    /// process holds still points at data that no longer exists on disk.
    /// </summary>
    Task RestoreAsync(string? folderPath, string fileName, CancellationToken cancellationToken);
}
