namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// What caused a <see cref="Entities.StockMovement"/>. Doubles as the movement's implied
/// direction and the kind of record (if any) it traces back to via
/// <see cref="Entities.StockMovement.ReferenceId"/>:
/// <see cref="GoodsReceived"/> → a <see cref="Entities.GoodsReceivedNote"/>,
/// <see cref="StockReleaseOut"/>/<see cref="StockReleaseIn"/> → a <see cref="Entities.StockRelease"/>,
/// <see cref="Adjustment"/> and <see cref="Consumption"/> stand alone.
/// </summary>
public enum StockMovementType
{
    /// <summary>Stock received from a supplier into the Main Store. Always a positive delta.</summary>
    GoodsReceived = 1,

    /// <summary>The Main Store side of a release to the kitchen. Always a negative delta.</summary>
    StockReleaseOut = 2,

    /// <summary>The Kitchen side of a release from the main store. Always a positive delta.</summary>
    StockReleaseIn = 3,

    /// <summary>A manual correction to match a physical stock count. Delta may be either sign.</summary>
    Adjustment = 4,

    /// <summary>Kitchen stock deducted per a menu item's recipe when a sale completes.</summary>
    Consumption = 5,
}