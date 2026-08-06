namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// Why a ticket was printed. The kitchen needs to tell "cook these" apart from "you are already
/// cooking this, the numbers changed" and "stop, this is off" — so amendments print as their own
/// tickets (POS-020) rather than silently reprinting the original.
/// </summary>
public enum KitchenTicketKind
{
    /// <summary>The first ticket for an order, printed on confirmation (POS-010).</summary>
    New = 1,

    /// <summary>Items added to an already-open order (POS-014).</summary>
    Addition = 2,

    /// <summary>A quantity changed on an item already sent (POS-015).</summary>
    Modification = 3,

    /// <summary>An item or the whole order was cancelled (POS-020, POS-021).</summary>
    Cancellation = 4,
}
