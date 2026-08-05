using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A record of stock received from a supplier into the Main Store. Recording one immediately
/// increases Main Store stock — there is no separate draft/confirm step, since a GRN is only
/// ever entered once the goods have actually arrived and been counted.
/// </summary>
/// <remarks>
/// Its lines are not stored here; they are the <see cref="StockMovement"/> rows sharing this
/// note's <see cref="BaseEntity.Id"/> as their <see cref="StockMovement.ReferenceId"/>.
/// </remarks>
public sealed class GoodsReceivedNote : BaseEntity
{
    public const int NotesMaxLength = 500;
    public const int MinQualityRating = 1;
    public const int MaxQualityRating = 5;

    // EF Core materialisation.
    private GoodsReceivedNote()
    {
    }

    private GoodsReceivedNote(
        Guid supplierId,
        Guid receivedByUserId,
        DateTime receivedAtUtc,
        string? notes,
        Guid? purchaseOrderId,
        int? qualityRating,
        bool hasIssue)
    {
        SupplierId = supplierId;
        ReceivedByUserId = receivedByUserId;
        ReceivedAtUtc = receivedAtUtc;
        Notes = NormaliseNotes(notes);
        PurchaseOrderId = purchaseOrderId;
        QualityRating = ValidateRating(qualityRating);
        HasIssue = hasIssue;
    }

    public Guid SupplierId { get; private set; }

    public Guid ReceivedByUserId { get; private set; }

    public DateTime ReceivedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    /// <summary>
    /// The Purchase Order this delivery fulfils, if any — a GRN can also stand alone for a
    /// delivery that never had a formal PO raised against it.
    /// </summary>
    public Guid? PurchaseOrderId { get; private set; }

    /// <summary>1 (worst) to 5 (best), optional.</summary>
    public int? QualityRating { get; private set; }

    /// <summary>Flags this delivery for the supplier performance view, e.g. a complaint was raised.</summary>
    public bool HasIssue { get; private set; }

    public static GoodsReceivedNote Create(
        Guid supplierId,
        Guid receivedByUserId,
        DateTime receivedAtUtc,
        string? notes = null,
        Guid? purchaseOrderId = null,
        int? qualityRating = null,
        bool hasIssue = false) =>
        new(supplierId, receivedByUserId, receivedAtUtc, notes, purchaseOrderId, qualityRating, hasIssue);

    private static string? NormaliseNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();

        return trimmed.Length > NotesMaxLength
            ? throw new ArgumentException($"Notes cannot exceed {NotesMaxLength} characters.", nameof(notes))
            : trimmed;
    }

    private static int? ValidateRating(int? rating) =>
        rating is null or >= MinQualityRating and <= MaxQualityRating
            ? rating
            : throw new ArgumentOutOfRangeException(
                nameof(rating), rating, $"Quality rating must be between {MinQualityRating} and {MaxQualityRating}.");
}