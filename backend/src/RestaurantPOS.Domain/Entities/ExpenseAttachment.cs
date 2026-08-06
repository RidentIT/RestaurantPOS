using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A receipt or invoice filed against an expense (EXP-037).
/// </summary>
/// <remarks>
/// Only the metadata lives in the database; the file itself sits in a folder beside it. A few
/// hundred phone photos of receipts would otherwise dwarf every other table in the install and
/// make each backup drag the lot along.
/// </remarks>
public sealed class ExpenseAttachment : BaseEntity
{
    public const int FileNameMaxLength = 255;
    public const int ContentTypeMaxLength = 120;
    public const int StoredPathMaxLength = 400;

    // EF Core materialisation.
    private ExpenseAttachment()
    {
    }

    internal ExpenseAttachment(
        Guid expenseId,
        string fileName,
        string storedPath,
        string contentType,
        long sizeBytes,
        Guid uploadedByUserId,
        DateTime uploadedAtUtc)
    {
        ExpenseId = expenseId;
        FileName = fileName;
        StoredPath = storedPath;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedByUserId = uploadedByUserId;
        UploadedAtUtc = uploadedAtUtc;
    }

    public Guid ExpenseId { get; private set; }

    /// <summary>The name the file had when it was chosen, shown back to the user.</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>Where it lives, relative to the attachments root — never an absolute path.</summary>
    public string StoredPath { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }
}
