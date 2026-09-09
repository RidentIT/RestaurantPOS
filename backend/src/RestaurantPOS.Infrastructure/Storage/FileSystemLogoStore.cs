using Microsoft.Extensions.Configuration;

using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Infrastructure.Storage;

/// <summary>
/// Keeps the restaurant's logo in a folder beside the database, the same way
/// <see cref="FileSystemExpenseAttachmentStore"/> keeps receipts.
/// </summary>
/// <remarks>
/// There is only ever one logo, so unlike a receipt it needs no database row of its own to track
/// an original file name or content type — both are recovered from the extension the file was
/// saved under, since only a fixed, known set of image types is ever accepted.
/// </remarks>
internal sealed class FileSystemLogoStore : ILogoStore
{
    private static readonly Dictionary<string, string> ContentTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
    };

    private readonly string _root;

    public FileSystemLogoStore(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Deliberately the same setting FileSystemExpenseAttachmentStore reads, with the same
        // fallback: BackupService walks that one folder recursively to decide what "attachments"
        // means for a backup archive, so the logo has to live somewhere under it to be backed up
        // and restored along with everything else, even if that path is ever customised.
        var configuredRoot = configuration["Expenses:AttachmentsPath"];
        var attachmentsRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(AppContext.BaseDirectory, "attachments")
            : Path.GetFullPath(configuredRoot);

        _root = Path.Combine(attachmentsRoot, "logo");
    }

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        Directory.CreateDirectory(_root);

        // A fresh name every upload, not a fixed "logo.png" overwritten in place — a browser or
        // this app's own cache would otherwise keep showing the old image under the same URL.
        var relativePath = $"{Guid.NewGuid():N}{SafeExtension(fileName)}";
        var absolutePath = Path.Combine(_root, relativePath);

        await using var file = File.Create(absolutePath);
        await content.CopyToAsync(file, cancellationToken);

        return relativePath;
    }

    public Task<StoredAttachment?> OpenAsync(string storedPath, CancellationToken cancellationToken)
    {
        var absolutePath = ResolveWithinRoot(storedPath);

        if (absolutePath is null || !File.Exists(absolutePath))
        {
            return Task.FromResult<StoredAttachment?>(null);
        }

        var contentType = ContentTypesByExtension.GetValueOrDefault(
            Path.GetExtension(absolutePath), "application/octet-stream");

        Stream stream = File.OpenRead(absolutePath);

        return Task.FromResult<StoredAttachment?>(new StoredAttachment(stream, contentType, "logo" + Path.GetExtension(absolutePath)));
    }

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken)
    {
        var absolutePath = ResolveWithinRoot(storedPath);

        if (absolutePath is not null && File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Turns a stored relative path into an absolute one, refusing anything that resolves outside
    /// the logo folder. A path only ever comes from this store's own <see cref="SaveAsync"/>, but
    /// a database that has been edited by hand should not be able to read arbitrary files.
    /// </summary>
    private string? ResolveWithinRoot(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        var candidate = Path.GetFullPath(Path.Combine(_root, storedPath));
        var root = Path.GetFullPath(_root);

        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }

    /// <summary>Keeps a recognisable extension without trusting the rest of the supplied name.</summary>
    private static string SafeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return ContentTypesByExtension.ContainsKey(extension) ? extension.ToLowerInvariant() : ".png";
    }
}
