using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// What an expense is for — Gas, Electricity, Staff Meals (EXP-010, EXP-011).
/// </summary>
/// <remarks>
/// A category may sit under another to give subcategories one level deep, which is enough to say
/// "Utilities → Electricity" without letting the tree grow into something nobody can total up.
/// Seeded categories are marked <see cref="IsSystem"/>: they can be renamed and re-budgeted, but
/// not deleted, so a restaurant cannot remove "Rent" and orphan a year of history.
/// </remarks>
public sealed class ExpenseCategory : BaseEntity
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 250;

    // EF Core materialisation.
    private ExpenseCategory()
    {
    }

    private ExpenseCategory(
        string name, string? description, decimal? monthlyBudget, Guid? parentCategoryId, bool isSystem)
    {
        Name = NormaliseName(name);
        Description = NormaliseDescription(description);
        MonthlyBudget = ValidateBudget(monthlyBudget);
        ParentCategoryId = parentCategoryId;
        IsSystem = isSystem;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>
    /// What the restaurant expects to spend here in a month. Null means unbudgeted, which
    /// suppresses the overspend alerts rather than treating the limit as zero (BR-EXP-015).
    /// </summary>
    public decimal? MonthlyBudget { get; private set; }

    /// <summary>The parent this sits under, or null for a top-level category.</summary>
    public Guid? ParentCategoryId { get; private set; }

    /// <summary>True for the categories shipped with the system, which cannot be deleted.</summary>
    public bool IsSystem { get; private set; }

    public bool IsActive { get; private set; }

    public static ExpenseCategory Create(
        string name,
        string? description = null,
        decimal? monthlyBudget = null,
        Guid? parentCategoryId = null,
        bool isSystem = false) =>
        new(name, description, monthlyBudget, parentCategoryId, isSystem);

    public void UpdateDetails(string name, string? description, decimal? monthlyBudget, Guid? parentCategoryId)
    {
        Name = NormaliseName(name);
        Description = NormaliseDescription(description);
        MonthlyBudget = ValidateBudget(monthlyBudget);
        ParentCategoryId = parentCategoryId;
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

    private static string? NormaliseDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();

        return trimmed.Length > DescriptionMaxLength
            ? throw new ArgumentException(
                $"Description cannot exceed {DescriptionMaxLength} characters.", nameof(description))
            : trimmed;
    }

    private static decimal? ValidateBudget(decimal? budget) =>
        budget is null or >= 0
            ? budget
            : throw new ArgumentOutOfRangeException(nameof(budget), budget, "A budget cannot be negative.");
}
