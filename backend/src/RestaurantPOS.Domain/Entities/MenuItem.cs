using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One size being set on a <see cref="MenuItem"/> — an existing variant to update when
/// <see cref="Id"/> is supplied, or a new one to add when it is null.
/// </summary>
public sealed record MenuItemVariantEdit(Guid? Id, string? Name, decimal Price);

/// <summary>
/// A sellable dish or drink. Recipe Management attaches an optional <see cref="Recipe"/> to each
/// of its <see cref="Variants"/>, which is how a sale eventually deducts kitchen stock — but not
/// every variant needs one (a bottled drink with no preparation, for example).
/// </summary>
public sealed class MenuItem : BaseEntity
{
    public const int NameMaxLength = 150;
    public const int CategoryMaxLength = 80;

    private readonly List<MenuItemVariant> _variants = [];

    // EF Core materialisation.
    private MenuItem()
    {
    }

    private MenuItem(string name, string category, IReadOnlyCollection<MenuItemVariantEdit> variants)
    {
        Name = NormaliseName(name);
        Category = NormaliseCategory(category);
        IsActive = true;
        ReplaceVariants(variants);
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Free-text grouping for the menu, e.g. "Rice & Curry", "Beverages".</summary>
    public string Category { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    /// <summary>Every size this item is sold in. Always at least one.</summary>
    public IReadOnlyCollection<MenuItemVariant> Variants => _variants.AsReadOnly();

    public static MenuItem Create(string name, string category, IReadOnlyCollection<MenuItemVariantEdit> variants) =>
        new(name, category, variants);

    public void UpdateDetails(string name, string category)
    {
        Name = NormaliseName(name);
        Category = NormaliseCategory(category);
    }

    /// <summary>
    /// Reconciles the item's sizes with a fresh list wholesale: a supplied <see cref="MenuItemVariantEdit.Id"/>
    /// updates that size, a null one adds a new size, and any existing size missing from the list
    /// is removed.
    /// </summary>
    /// <remarks>
    /// Removing a size that has ever actually been sold is the caller's job to have refused before
    /// this runs — the aggregate itself has no visibility into order history, so it can only
    /// enforce what a size list has to look like in isolation: at least one size, and every size
    /// named once there is more than one to tell apart.
    /// </remarks>
    public void ReplaceVariants(IReadOnlyCollection<MenuItemVariantEdit> variants)
    {
        ArgumentNullException.ThrowIfNull(variants);

        if (variants.Count == 0)
        {
            throw new ArgumentException("A menu item must have at least one size.", nameof(variants));
        }

        if (variants.Count > 1 && variants.Any(v => string.IsNullOrWhiteSpace(v.Name)))
        {
            throw new ArgumentException(
                "Every size needs its own name once there is more than one size.", nameof(variants));
        }

        var names = variants
            .Where(v => !string.IsNullOrWhiteSpace(v.Name))
            .Select(v => v.Name!.Trim().ToLowerInvariant())
            .ToList();

        if (names.Distinct().Count() != names.Count)
        {
            throw new ArgumentException("Two sizes on the same item cannot share a name.", nameof(variants));
        }

        var keepIds = variants.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet();
        _variants.RemoveAll(v => !keepIds.Contains(v.Id));

        var sortOrder = 0;

        foreach (var input in variants)
        {
            if (input.Id is { } id)
            {
                var existing = _variants.First(v => v.Id == id);
                existing.UpdateDetails(input.Name, input.Price, sortOrder);
            }
            else
            {
                _variants.Add(new MenuItemVariant(Id, input.Name, input.Price, sortOrder));
            }

            sortOrder++;
        }
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
}
