namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One raw material and the quantity of it a single unit of the parent recipe's menu item
/// consumes, expressed in that raw material's own unit of measurement.
/// </summary>
public sealed class RecipeLine
{
    // EF Core materialisation.
    private RecipeLine()
    {
    }

    internal RecipeLine(Guid recipeId, Guid rawMaterialId, decimal quantity)
    {
        RecipeId = recipeId;
        RawMaterialId = rawMaterialId;
        Quantity = quantity > 0
            ? quantity
            : throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero.");
    }

    public Guid RecipeId { get; private set; }

    public Guid RawMaterialId { get; private set; }

    /// <summary>Always greater than zero (BR-REC-003); fractional amounts are allowed (REC-012).</summary>
    public decimal Quantity { get; private set; }
}