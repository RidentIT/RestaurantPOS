namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// The unit a raw material's stock is counted in. A raw material has exactly one unit for its
/// entire lifetime — recipes consuming it must specify quantities in this same unit, so no
/// conversion table is needed anywhere in the system.
/// </summary>
public enum UnitOfMeasurement
{
    Kilogram = 1,
    Gram = 2,
    Liter = 3,
    Milliliter = 4,
    Piece = 5,
    Bottle = 6,
    Packet = 7,
}