using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A sellable dish or drink. Recipe Management attaches an optional <see cref="Recipe"/> to
/// this, which is how a sale eventually deducts kitchen stock — but not every menu item needs
/// one (a bottled drink with no preparation, for example).
/// </summary>
public sealed class MenuItem : BaseEntity
{
    public const int NameMaxLength = 150;
    public const int CategoryMaxLength = 80;

    // EF Core materialisation.
    private MenuItem()
    {
    }

    private MenuItem(string name, string category, decimal price)
    {
        Name = NormaliseName(name);
        Category = NormaliseCategory(category);
        Price = ValidatePrice(price);
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Free-text grouping for the menu, e.g. "Rice & Curry", "Beverages".</summary>
    public string Category { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public bool IsActive { get; private set; }

    public static MenuItem Create(string name, string category, decimal price) =>
        new(name, category, price);

    public void UpdateDetails(string name, string category, decimal price)
    {
        Name = NormaliseName(name);
        Category = NormaliseCategory(category);
        Price = ValidatePrice(price);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string NormaliseName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmed = name.Trim();

        return trimmed.Length > NameMaxLength
            ? throw new ArgumentException($"Name cannot exceed {NameMaxLength} characters.", nameof(name))
            : trimmed;
    }

    private static string NormaliseCategory(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        var trimmed = category.Trim();

        return trimmed.Length > CategoryMaxLength
            ? throw new ArgumentException($"Category cannot exceed {CategoryMaxLength} characters.", nameof(category))
            : trimmed;
    }

    private static decimal ValidatePrice(decimal price) =>
        price >= 0
            ? price
            : throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative.");
}