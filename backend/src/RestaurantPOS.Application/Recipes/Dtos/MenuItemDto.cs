namespace RestaurantPOS.Application.Recipes.Dtos;

/// <summary>One sellable size of a menu item, as presented to the client.</summary>
public sealed record MenuItemVariantDto(Guid Id, string? Name, decimal Price, bool HasRecipe);

/// <summary>A menu item as presented to the client, with every size it's sold in.</summary>
public sealed record MenuItemDto(
    Guid Id,
    string Name,
    string Category,
    bool IsActive,
    IReadOnlyCollection<MenuItemVariantDto> Variants,
    DateTime CreatedAtUtc);
