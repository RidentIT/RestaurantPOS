namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>A stored receipt file, ready to be streamed back to the browser.</summary>
public sealed record StoredAttachment(Stream Content, string ContentType, string FileName);

/// <summary>
/// Where receipt and invoice files live.
/// </summary>
/// <remarks>
/// Files sit in a folder beside the database rather than inside it: a year of phone photos would
/// otherwise dwarf every other table and drag on every backup. Paths handed back are relative to
/// the attachments root, so moving or copying the data folder does not strand them.
/// </remarks>
public interface IExpenseAttachmentStore
{
    /// <summary>Writes a file and returns its path relative to the attachments root.</summary>
    Task<string> SaveAsync(
        Stream content, string fileName, DateOnly expenseDate, CancellationToken cancellationToken);

    /// <summary>Opens a stored file, or returns null when it is no longer on disk.</summary>
    Task<StoredAttachment?> OpenAsync(
        string storedPath, string contentType, string fileName, CancellationToken cancellationToken);

    /// <summary>Deletes a stored file. Missing files are not an error — the goal is that it is gone.</summary>
    Task DeleteAsync(string storedPath, CancellationToken cancellationToken);
}
