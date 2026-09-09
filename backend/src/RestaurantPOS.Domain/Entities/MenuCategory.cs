using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A known name for grouping the menu — "Rice &amp; Curry", "Beverages", "Short Eats".
/// </summary>
/// <remarks>
/// <see cref="MenuItem.Category"/> stays a plain string rather than a foreign key to this table:
/// nothing here is enforced at the database level, so an old row or a future direct API call can
/// still write a category this list has never heard of. What this table actually buys is a place
/// for a category to exist <em>before</em> any menu item uses it — an admin loading a client's
/// full category list on day one, not just whatever's been typed into a dish so far — and a
/// picker that suggests real names instead of an admin retyping "Rice &amp; Curry" by hand and
/// getting it slightly wrong the third time.
/// </remarks>
public sealed class MenuCategory : BaseEntity
{
    public const int NameMaxLength = 80;

    // EF Core materialisation.
    private MenuCategory()
    {
    }

    private MenuCategory(string name)
    {
        Name = Normalise(name);
    }

    public string Name { get; private set; } = string.Empty;

    public static MenuCategory Create(string name) => new(name);

    private static string Normalise(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmed = name.Trim();

        return trimmed.Length > NameMaxLength
            ? throw new ArgumentException($"Category cannot exceed {NameMaxLength} characters.", nameof(name))
            : trimmed;
    }
}
