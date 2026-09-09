using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Mappings;

/// <summary>Projects a <see cref="MenuItem"/> aggregate onto its read model, one item at a time.</summary>
public static class MenuItemMappings
{
    public static async Task<MenuItemDto> ToDtoAsync(
        this MenuItem menuItem, IAppDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(menuItem);
        ArgumentNullException.ThrowIfNull(db);

        var variantIds = menuItem.Variants.Select(v => v.Id).ToList();

        var recipedVariantIds = await db.Recipes.AsNoTracking()
            .Where(r => variantIds.Contains(r.MenuItemVariantId))
            .Select(r => r.MenuItemVariantId)
            .ToListAsync(cancellationToken);

        var recipedSet = recipedVariantIds.ToHashSet();

        return menuItem.ToDto(recipedSet);
    }

    /// <summary>
    /// The pure projection, for callers (like a list screen) that have already worked out which
    /// variants across many items have a recipe in a single bulk query of their own.
    /// </summary>
    public static MenuItemDto ToDto(this MenuItem menuItem, IReadOnlySet<Guid> recipedVariantIds)
    {
        ArgumentNullException.ThrowIfNull(menuItem);
        ArgumentNullException.ThrowIfNull(recipedVariantIds);

        return new MenuItemDto(
            menuItem.Id,
            menuItem.Name,
            menuItem.Category,
            menuItem.IsActive,
            [.. menuItem.Variants
                .OrderBy(v => v.SortOrder)
                .Select(v => new MenuItemVariantDto(v.Id, v.Name, v.Price, recipedVariantIds.Contains(v.Id)))],
            menuItem.CreatedAtUtc);
    }
}
