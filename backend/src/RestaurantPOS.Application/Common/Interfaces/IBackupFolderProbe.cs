namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>
/// Checks whether a backup folder is actually usable before it is saved — a local path, a USB
/// drive, or a folder synced by something like OneDrive Desktop all need the same thing: the till
/// can create and write files there right now.
/// </summary>
public interface IBackupFolderProbe
{
    /// <summary>Creates the folder if missing and attempts to write and delete a probe file in it.</summary>
    bool CanWrite(string folderPath);
}
