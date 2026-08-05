using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A transfer of stock from the Main Store to the Kitchen. Approval is immediate rather than a
/// separate pending state: the releasing staff member and an administrator's approval PIN are
/// both supplied in the same request, so a release exists only once it is already approved and
/// its stock movements have already been applied (INV-008 through INV-012).
/// </summary>
/// <remarks>
/// Its lines are not stored here; they are the <see cref="StockMovement"/> rows (one
/// <see cref="Enums.StockMovementType.StockReleaseOut"/> and one
/// <see cref="Enums.StockMovementType.StockReleaseIn"/> per raw material) sharing this release's
/// <see cref="BaseEntity.Id"/> as their <see cref="StockMovement.ReferenceId"/>.
/// </remarks>
public sealed class StockRelease : BaseEntity
{
    public const int NotesMaxLength = 500;

    // EF Core materialisation.
    private StockRelease()
    {
    }

    private StockRelease(
        Guid requestedByUserId,
        DateTime requestedAtUtc,
        Guid approvedByUserId,
        DateTime approvedAtUtc,
        string? notes)
    {
        RequestedByUserId = requestedByUserId;
        RequestedAtUtc = requestedAtUtc;
        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = approvedAtUtc;
        Notes = NormaliseNotes(notes);
    }

    public Guid RequestedByUserId { get; private set; }

    public DateTime RequestedAtUtc { get; private set; }

    /// <summary>The administrator whose approval PIN authorised this release.</summary>
    public Guid ApprovedByUserId { get; private set; }

    public DateTime ApprovedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public static StockRelease Create(
        Guid requestedByUserId,
        DateTime requestedAtUtc,
        Guid approvedByUserId,
        DateTime approvedAtUtc,
        string? notes = null) =>
        new(requestedByUserId, requestedAtUtc, approvedByUserId, approvedAtUtc, notes);

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
}