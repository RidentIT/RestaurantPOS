using Microsoft.Extensions.Configuration;

using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Infrastructure.Storage;

/// <summary>
/// Keeps receipt files in a folder beside the database.
/// </summary>
/// <remarks>
/// Files are filed under year/month so a folder never grows to tens of thousands of entries, and
/// each is given a fresh GUID name. That name is deliberately not the one the user chose: two
/// receipts called "IMG_0001.jpg" would otherwise collide, and a crafted name like
/// <c>..\..\appsettings.json</c> would let an upload escape the folder entirely. The original
/// name is kept in the database and handed back on download, so the user never sees the change.
/// </remarks>
internal sealed class FileSystemExpenseAttachmentStore : IExpenseAttachmentStore
{
    private readonly string _root;

    public FileSystemExpenseAttachmentStore(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration["Expenses:AttachmentsPath"];

        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "attachments")
            : Path.GetFullPath(configured);
    }

    public async Task<string> SaveAsync(
        Stream content, string fileName, DateOnly expenseDate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var relativeFolder = Path.Combine(expenseDate.Year.ToString("0000"), expenseDate.Month.ToString("00"));
        var extension = SafeExtension(fileName);
        var relativePath = Path.Combine(relativeFolder, $"{Guid.NewGuid():N}{extension}");

        var absoluteFolder = Path.Combine(_root, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var absolutePath = Path.Combine(_root, relativePath);

        await using var file = File.Create(absolutePath);
        await content.CopyToAsync(file, cancellationToken);

        // Stored with forward slashes so a database copied between machines still resolves.
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    public Task<StoredAttachment?> OpenAsync(
        string storedPath, string contentType, string fileName, CancellationToken cancellationToken)
    {
        var absolutePath = ResolveWithinRoot(storedPath);

        if (absolutePath is null || !File.Exists(absolutePath))
        {
            return Task.FromResult<StoredAttachment?>(null);
        }

        Stream stream = File.OpenRead(absolutePath);

        return Task.FromResult<StoredAttachment?>(new StoredAttachment(stream, contentType, fileName));
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
    /// the attachments root. A path only ever comes from this store's own <see cref="SaveAsync"/>,
    /// but a database that has been edited by hand should not be able to read arbitrary files.
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

        return extension.Length is > 1 and <= 10 && extension.All(c => char.IsAsciiLetterOrDigit(c) || c == '.')
            ? extension.ToLowerInvariant()
            : string.Empty;
    }
}
