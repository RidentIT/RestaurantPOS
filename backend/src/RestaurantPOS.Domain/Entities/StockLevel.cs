using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// The current on-hand quantity of one raw material in one store. This is a derived balance,
/// never edited directly (BR-INV-006) — it only changes through <see cref="ApplyDelta"/>, called
/// by the application layer alongside writing the <see cref="StockMovement"/> that explains why.
/// </summary>
public sealed class StockLevel
{
    // EF Core materialisation.
    private StockLevel()
    {
    }

    public StockLevel(Guid rawMaterialId, StoreType store)
    {
        RawMaterialId = rawMaterialId;
        Store = store;
        QuantityOnHand = 0;
    }

    public Guid RawMaterialId { get; private set; }

    public StoreType Store { get; private set; }

    public decimal QuantityOnHand { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Applies a signed change to the balance. This is a last-resort guard, not the primary
    /// safety check — callers are expected to have already confirmed the result would not go
    /// negative (the application layer blocks that with a friendly error) before ever reaching
    /// here.
    /// </summary>
    public void ApplyDelta(decimal delta, DateTime nowUtc)
    {
        var updated = QuantityOnHand + delta;

        if (updated < 0)
        {
            throw new InvalidOperationException(
                $"Applying this change would take the balance negative ({updated}).");
        }

        QuantityOnHand = updated;
        UpdatedAtUtc = nowUtc;
    }
}