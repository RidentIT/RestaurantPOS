using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by restaurant settings and backup use cases.</summary>
public static class SettingsErrors
{
    public static readonly Error NotConfigured =
        Error.Conflict("Settings.NotConfigured", "Restaurant settings have not been set up yet.");

    public static readonly Error InvalidPercentage =
        Error.Validation("Settings.InvalidPercentage", "A percentage must be between 0 and 100.");

    public static readonly Error InvalidPinPolicy =
        Error.Validation(
            "Settings.InvalidPinPolicy", "Attempts must be between 1 and 10, and the lockout between 1 and 60 minutes.");

    public static readonly Error InvalidRetention =
        Error.Validation("Settings.InvalidRetention", "Retention must be between 1 and 60 backups.");

    public static Error BackupNotFound(string fileName) =>
        Error.NotFound("Backup.NotFound", $"No backup was found named '{fileName}'.");

    public static readonly Error BackupFolderNotWritable =
        Error.Validation(
            "Backup.FolderNotWritable", "The backup folder could not be written to. Check the path and try again.");

    public static readonly Error RestoreConfirmationMismatch =
        Error.Validation(
            "Backup.RestoreConfirmationMismatch", "Type RESTORE exactly to confirm — this replaces every record.");

    public static readonly Error RestoreFailed =
        Error.Conflict("Backup.RestoreFailed", "The backup could not be restored. No changes were made.");

    public static readonly Error InvalidBackupArchive =
        Error.Validation("Backup.InvalidArchive", "That file is not a valid backup archive.");
}
