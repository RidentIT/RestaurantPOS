using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One sellable size of a menu item — what actually has a price and a recipe.
/// </summary>
/// <remarks>
/// Most dishes need only one of these, with no name of its own ("Bottled Water" is just
/// "Bottled Water" at one price). A dish sold in sizes — Rice's Normal and Full — gets one
/// variant per size instead, each independently priced and independently reciped, since a Full
/// portion doesn't just cost more, it uses more of every ingredient too. Every
/// <see cref="MenuItem"/> always has at least one variant; there is no such thing as a menu item
/// with nothing to actually order.
/// </remarks>
public sealed class MenuItemVariant : BaseEntity
{
    public const int NameMaxLength = 40;

    // EF Core materialisation.
    private MenuItemVariant()
    {
    }

    internal MenuItemVariant(Guid menuItemId, string? name, decimal price, int sortOrder)
    {
        MenuItemId = menuItemId;
        Name = NormaliseName(name);
        Price = ValidatePrice(price);
        SortOrder = sortOrder;
    }

    public Guid MenuItemId { get; private set; }

    /// <summary>"Normal", "Full" — or null for a menu item with only one size, which needs no label.</summary>
    public string? Name { get; private set; }

    public decimal Price { get; private set; }

    /// <summary>Where this size sits among its own item's other sizes — Normal before Full, not alphabetical.</summary>
    public int SortOrder { get; private set; }

    internal void UpdateDetails(string? name, decimal price, int sortOrder)
    {
        Name = NormaliseName(name);
        Price = ValidatePrice(price);
        SortOrder = sortOrder;
    }

    private static decimal ValidatePrice(decimal price) =>
        price >= 0 ? price : throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative.");

    private static string? NormaliseName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();

        return trimmed.Length > NameMaxLength
            ? throw new ArgumentException($"A size name cannot exceed {NameMaxLength} characters.", nameof(name))
            : trimmed;
    }
}
