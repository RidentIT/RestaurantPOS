using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Mappings;

/// <summary>Projects <see cref="Recipe"/> aggregates onto their read model.</summary>
public static class RecipeMappings
{
    /// <summary>
    /// Builds the DTO, denormalising each line's raw material name and unit in — a lookup
    /// rather than a plain property mapping, since that data lives on <see cref="RawMaterial"/>,
    /// not on <see cref="RecipeLine"/> itself.
    /// </summary>
    public static async Task<RecipeDto> ToDtoAsync(
        this Recipe recipe, IAppDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(db);

        var rawMaterialIds = recipe.Lines.Select(l => l.RawMaterialId).ToList();

        var rawMaterials = await db.RawMaterials.AsNoTracking()
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var lines = recipe.Lines
            .Select(l => new RecipeLineDto(
                l.RawMaterialId,
                rawMaterials[l.RawMaterialId].Name,
                rawMaterials[l.RawMaterialId].UnitOfMeasurement,
                l.Quantity))
            .OrderBy(l => l.RawMaterialName)
            .ToList();

        return new RecipeDto(
            recipe.Id, recipe.MenuItemVariantId, recipe.IsEnabled, lines, recipe.CreatedAtUtc, recipe.UpdatedAtUtc);
    }
}