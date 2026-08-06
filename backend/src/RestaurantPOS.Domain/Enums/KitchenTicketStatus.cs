namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// How far the kitchen has got with one printed ticket. A table shows its least-advanced ticket,
/// so a table where one dish is still queued reads as preparing rather than ready.
/// </summary>
public enum KitchenTicketStatus
{
    /// <summary>Printed and queued. Nobody has started cooking it.</summary>
    New = 1,

    /// <summary>Being cooked.</summary>
    Preparing = 2,

    /// <summary>Cooked and waiting to go out.</summary>
    Ready = 3,

    /// <summary>Carried to the table.</summary>
    Served = 4,
}
