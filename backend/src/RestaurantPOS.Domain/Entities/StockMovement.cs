using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One line in the permanent stock ledger: every quantity change to every raw material, in
/// either store, for any reason, is one row here (BR-INV-005, BR-INV-007). This is the single
/// source of truth movements are reconstructed from — a GRN's or release's "lines" are simply
/// the movements sharing its <see cref="ReferenceId"/>, rather than being duplicated into their
/// own line tables.
/// </summary>
public sealed class StockMovement : BaseEntity
{
    public const int NotesMaxLength = 500;

    // EF Core materialisation.
    private StockMovement()
    {
    }

    public StockMovement(
        Guid rawMaterialId,
        StoreType store,
        decimal quantityDelta,
        StockMovementType type,
        Guid? referenceId,
        Guid performedByUserId,
        DateTime occurredAtUtc,
        string? notes)
    {
        if (quantityDelta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityDelta), quantityDelta, "A movement must change the balance.");
        }

        RawMaterialId = rawMaterialId;
        Store = store;
        QuantityDelta = quantityDelta;
        Type = type;
        ReferenceId = referenceId;
        PerformedByUserId = performedByUserId;
        OccurredAtUtc = occurredAtUtc;
        Notes = NormaliseNotes(notes);
    }

    public Guid RawMaterialId { get; private set; }

    public StoreType Store { get; private set; }

    /// <summary>Signed change to the balance: positive increases stock, negative decreases it.</summary>
    public decimal QuantityDelta { get; private set; }

    public StockMovementType Type { get; private set; }

    /// <summary>
    /// The id of the record this movement traces back to — a <see cref="GoodsReceivedNote"/> or
    /// <see cref="StockRelease"/>. Null for an <see cref="StockMovementType.Adjustment"/> or
    /// <see cref="StockMovementType.Consumption"/>, which have no separate header record.
    /// </summary>
    public Guid? ReferenceId { get; private set; }

    public Guid PerformedByUserId { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>Required for an adjustment (the reason for the correction); optional otherwise.</summary>
    public string? Notes { get; private set; }

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