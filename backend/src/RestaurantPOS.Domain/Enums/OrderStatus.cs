namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// Where an order sits in its lifecycle (POS-033). Confirming a draft prints the first KOT and
/// opens the order; it then stays <see cref="Open"/> — accepting further items without approval
/// (BR-POS-005) — until the cashier starts a checkout.
/// </summary>
public enum OrderStatus
{
    /// <summary>Being built at the till. Nothing has reached the kitchen and no table is held.</summary>
    Draft = 1,

    /// <summary>Confirmed and sent to the kitchen. Still accepting new items.</summary>
    Open = 2,

    /// <summary>Payment in progress. Items are frozen so the bill cannot move under the cashier.</summary>
    Checkout = 3,

    /// <summary>Paid in full, receipt issued, table released (POS-032).</summary>
    Completed = 4,

    /// <summary>Abandoned before payment. Requires an approval PIN (BR-POS-009).</summary>
    Cancelled = 5,
}
