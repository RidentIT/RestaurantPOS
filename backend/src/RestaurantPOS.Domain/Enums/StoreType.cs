namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// The two fixed stock locations the restaurant operates. There are exactly two by design —
/// suppliers replenish only <see cref="MainStore"/>, and <see cref="Kitchen"/> is replenished
/// only by an approved release from the main store (see BR-INV-001 through BR-INV-003).
/// </summary>
public enum StoreType
{
    MainStore = 1,
    Kitchen = 2,
}