namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>
/// The restaurant's own details, as printed at the top of every receipt (POS-027). Supplied by
/// configuration rather than compiled in so the same build can be installed for a second branch
/// without a code change.
/// </summary>
public interface IRestaurantProfile
{
    string Name { get; }

    string AddressLine1 { get; }

    string? AddressLine2 { get; }

    string? City { get; }

    string? Phone { get; }
}
