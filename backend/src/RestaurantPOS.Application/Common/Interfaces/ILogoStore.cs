namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>
/// Where the restaurant's logo lives on disk — one file, replaced wholesale each time an
/// administrator uploads a new one. Sits beside the database the same way receipt attachments and
/// backups do, so moving or copying the data folder never strands it.
/// </summary>
public interface ILogoStore
{
    /// <summary>Writes the logo and returns its path relative to the store's root.</summary>
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// Opens the stored logo, or returns null when it is no longer on disk. Unlike an expense
    /// receipt, no separate row tracks the logo's original content type or file name — there is
    /// only ever one, so the store works both out from the extension it saved the file under.
    /// </summary>
    Task<StoredAttachment?> OpenAsync(string storedPath, CancellationToken cancellationToken);

    /// <summary>Deletes a stored logo file. Missing files are not an error — the goal is that it is gone.</summary>
    Task DeleteAsync(string storedPath, CancellationToken cancellationToken);
}
