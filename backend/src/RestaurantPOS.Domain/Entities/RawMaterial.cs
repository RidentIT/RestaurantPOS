using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// An ingredient tracked in stock. Its <see cref="UnitOfMeasurement"/> is fixed for the
/// material's lifetime — recipes and every stock movement referencing it are always expressed
/// in this same unit, so the system never needs to convert between units.
/// </summary>
public sealed class RawMaterial : BaseEntity
{
    public const int NameMaxLength = 150;

    // EF Core materialisation.
    private RawMaterial()
    {
    }

    private RawMaterial(
        string name,
        UnitOfMeasurement unitOfMeasurement,
        decimal? mainStoreReorderLevel,
        decimal? kitchenParLevel)
    {
        Name = NormaliseName(name);
        UnitOfMeasurement = unitOfMeasurement;
        MainStoreReorderLevel = ValidateThreshold(mainStoreReorderLevel, nameof(mainStoreReorderLevel));
        KitchenParLevel = ValidateThreshold(kitchenParLevel, nameof(kitchenParLevel));
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public UnitOfMeasurement UnitOfMeasurement { get; private set; }

    /// <summary>
    /// When Main Store stock falls to or below this, the store is flagged low so staff know to
    /// call the supplier. Null means no threshold is configured for this material.
    /// </summary>
    public decimal? MainStoreReorderLevel { get; private set; }

    /// <summary>
    /// When Kitchen stock falls to or below this, the kitchen is flagged low so staff know to
    /// request a release from the Main Store. Null means no threshold is configured.
    /// </summary>
    public decimal? KitchenParLevel { get; private set; }

    public bool IsActive { get; private set; }

    public static RawMaterial Create(
        string name,
        UnitOfMeasurement unitOfMeasurement,
        decimal? mainStoreReorderLevel = null,
        decimal? kitchenParLevel = null) =>
        new(name, unitOfMeasurement, mainStoreReorderLevel, kitchenParLevel);

    public void UpdateDetails(
        string name,
        UnitOfMeasurement unitOfMeasurement,
        decimal? mainStoreReorderLevel,
        decimal? kitchenParLevel)
    {
        Name = NormaliseName(name);
        UnitOfMeasurement = unitOfMeasurement;
        MainStoreReorderLevel = ValidateThreshold(mainStoreReorderLevel, nameof(mainStoreReorderLevel));
        KitchenParLevel = ValidateThreshold(kitchenParLevel, nameof(kitchenParLevel));
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

    private static decimal? ValidateThreshold(decimal? threshold, string paramName) =>
        threshold is null or >= 0
            ? threshold
            : throw new ArgumentOutOfRangeException(paramName, threshold, "A threshold cannot be negative.");
}