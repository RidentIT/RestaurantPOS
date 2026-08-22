using System.Data;
using System.IO.Compression;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Infrastructure.Persistence;

namespace RestaurantPOS.Infrastructure.DataBackup;

/// <summary>
/// Backs the restaurant's database and receipt attachments up to a single zip archive, and can
/// restore one back.
/// </summary>
/// <remarks>
/// Uses SQLite's own <c>VACUUM INTO</c> rather than copying the file by hand, which is what makes
/// a backup safe to take while the till is still serving orders: a raw file copy could catch the
/// database mid-write, but <c>VACUUM INTO</c> always produces a complete, consistent snapshot.
/// </remarks>
internal sealed class BackupService(AppDbContext db, IConfiguration configuration) : IBackupService
{
    private const string ArchivePrefix = "Backup-";
    private const string DatabaseEntryName = "restaurantpos.db";
    private const string AttachmentsEntryPrefix = "attachments/";

    private static readonly byte[] SqliteHeader = "SQLite format 3\0"u8.ToArray();

    public Task<IReadOnlyCollection<BackupFileInfo>> ListAsync(string? folderPath, CancellationToken cancellationToken)
    {
        var folder = ResolveFolder(folderPath);

        if (!Directory.Exists(folder))
        {
            return Task.FromResult<IReadOnlyCollection<BackupFileInfo>>([]);
        }

        IReadOnlyCollection<BackupFileInfo> files =
            [.. Directory.GetFiles(folder, $"{ArchivePrefix}*.zip").Select(Describe)];

        return Task.FromResult(files);
    }

    public async Task<BackupFileInfo> CreateAsync(
        string? folderPath, int retentionCount, CancellationToken cancellationToken)
    {
        var folder = ResolveFolder(folderPath);
        Directory.CreateDirectory(folder);

        var tempDb = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

        try
        {
            await VacuumIntoAsync(tempDb, cancellationToken);

            var fileName = $"{ArchivePrefix}{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
            var zipPath = Path.Combine(folder, fileName);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(tempDb, DatabaseEntryName, CompressionLevel.Optimal);
                AddAttachments(archive);
            }

            Prune(folder, retentionCount);

            return Describe(zipPath);
        }
        finally
        {
            if (File.Exists(tempDb))
            {
                File.Delete(tempDb);
            }
        }
    }

    public async Task RestoreAsync(string? folderPath, string fileName, CancellationToken cancellationToken)
    {
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
        {
            throw new InvalidOperationException("Invalid backup file name.");
        }

        var folder = ResolveFolder(folderPath);
        var zipPath = Path.Combine(folder, fileName);

        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException("Backup archive not found.", zipPath);
        }

        var tempDb = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

        try
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                var dbEntry = archive.GetEntry(DatabaseEntryName)
                    ?? throw new InvalidDataException("The archive does not contain a database.");

                dbEntry.ExtractToFile(tempDb, overwrite: true);
                ValidateSqliteFile(tempDb);

                var livePath = await GetDatabasePathAsync(cancellationToken);

                // Every connection in this process, including the one this request is using, has
                // to let go of the file before it can be overwritten — the process is about to be
                // shut down anyway, so nothing here needs that connection again.
                await db.Database.CloseConnectionAsync();
                SqliteConnection.ClearAllPools();

                foreach (var suffix in new[] { "-wal", "-shm" })
                {
                    var sidecar = livePath + suffix;
                    if (File.Exists(sidecar))
                    {
                        File.Delete(sidecar);
                    }
                }

                File.Copy(tempDb, livePath, overwrite: true);

                RestoreAttachments(archive);
            }
        }
        finally
        {
            if (File.Exists(tempDb))
            {
                File.Delete(tempDb);
            }
        }
    }

    private async Task VacuumIntoAsync(string targetPath, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "VACUUM INTO $path";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$path";
        parameter.Value = targetPath;
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<string> GetDatabasePathAsync(CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT file FROM pragma_database_list WHERE name = 'main'";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result as string
            ?? throw new InvalidOperationException("Could not resolve the live database's file path.");
    }

    private void AddAttachments(ZipArchive archive)
    {
        var root = ResolveAttachmentsRoot();

        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            archive.CreateEntryFromFile(file, AttachmentsEntryPrefix + relative, CompressionLevel.Optimal);
        }
    }

    private void RestoreAttachments(ZipArchive archive)
    {
        var root = ResolveAttachmentsRoot();
        var fullRoot = Path.GetFullPath(root);

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith(AttachmentsEntryPrefix, StringComparison.Ordinal)
                || entry.FullName.EndsWith('/'))
            {
                continue;
            }

            var relative = entry.FullName[AttachmentsEntryPrefix.Length..];
            var destination = Path.GetFullPath(Path.Combine(fullRoot, relative));

            if (!destination.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, overwrite: true);
        }
    }

    /// <summary>Deletes the oldest archives once there are more than <paramref name="retentionCount"/>.</summary>
    private static void Prune(string folder, int retentionCount)
    {
        var stale = Directory.GetFiles(folder, $"{ArchivePrefix}*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .Skip(retentionCount);

        foreach (var file in stale)
        {
            file.Delete();
        }
    }

    private static void ValidateSqliteFile(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[SqliteHeader.Length];
        var read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

        if (read != header.Length || !header.SequenceEqual(SqliteHeader))
        {
            throw new InvalidDataException("That file is not a valid SQLite database.");
        }
    }

    private static BackupFileInfo Describe(string path)
    {
        var info = new FileInfo(path);

        return new BackupFileInfo(info.Name, info.CreationTimeUtc, info.Length);
    }

    private static string ResolveFolder(string? folderPath) =>
        string.IsNullOrWhiteSpace(folderPath)
            ? Path.Combine(AppContext.BaseDirectory, "Backups")
            : Path.GetFullPath(folderPath);

    private string ResolveAttachmentsRoot()
    {
        var configured = configuration["Expenses:AttachmentsPath"];

        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "attachments")
            : Path.GetFullPath(configured);
    }
}
