using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// The bill of materials for one menu item size: which raw materials a single sale of it
/// consumes, and how much of each. At most one recipe exists per size (enforced by a unique
/// index on <see cref="MenuItemVariantId"/> — see BR-REC-001), and not every size has one at all.
/// A size sold in a bigger portion gets its own recipe rather than the same one scaled up — a
/// Full doesn't always use exactly proportionally more of everything.
/// </summary>
public sealed class Recipe : BaseEntity
{
    private readonly List<RecipeLine> _lines = [];

    // EF Core materialisation.
    private Recipe()
    {
    }

    private Recipe(Guid menuItemVariantId, IEnumerable<(Guid RawMaterialId, decimal Quantity)> lines)
    {
        MenuItemVariantId = menuItemVariantId;
        IsEnabled = true;
        SetLines(lines);
    }

    public Guid MenuItemVariantId { get; private set; }

    /// <summary>Disabled recipes cannot be used for new sales (BR-REC-006) but stay editable.</summary>
    public bool IsEnabled { get; private set; }

    public IReadOnlyCollection<RecipeLine> Lines => _lines.AsReadOnly();

    /// <summary>Creates a recipe. Requires at least one line (BR-REC-002).</summary>
    public static Recipe Create(Guid menuItemVariantId, IEnumerable<(Guid RawMaterialId, decimal Quantity)> lines) =>
        new(menuItemVariantId, lines);

    /// <summary>Replaces every ingredient line wholesale (REC-006: recipes are editable at any time).</summary>
    public void ReplaceLines(IEnumerable<(Guid RawMaterialId, decimal Quantity)> lines) => SetLines(lines);

    public void Enable() => IsEnabled = true;

    public void Disable() => IsEnabled = false;

    private void SetLines(IEnumerable<(Guid RawMaterialId, decimal Quantity)> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var materialised = lines.ToList();

        if (materialised.Count == 0)
        {
            throw new ArgumentException("A recipe must contain at least one raw material.", nameof(lines));
        }

        if (materialised.Select(l => l.RawMaterialId).Distinct().Count() != materialised.Count)
        {
            throw new ArgumentException("A raw material cannot appear more than once in the same recipe.", nameof(lines));
        }

        _lines.Clear();
        _lines.AddRange(materialised.Select(l => new RecipeLine(Id, l.RawMaterialId, l.Quantity)));
    }
}