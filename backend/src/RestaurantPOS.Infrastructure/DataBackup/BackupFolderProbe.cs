using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Infrastructure.DataBackup;

internal sealed class BackupFolderProbe : IBackupFolderProbe
{
    public bool CanWrite(string folderPath)
    {
        try
        {
            Directory.CreateDirectory(folderPath);

            var probePath = Path.Combine(folderPath, $".probe-{Guid.NewGuid():N}");
            File.WriteAllBytes(probePath, [0]);
            File.Delete(probePath);

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException
            or ArgumentException or System.Security.SecurityException)
        {
            return false;
        }
    }
}
