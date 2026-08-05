namespace RestaurantPOS.Application.Recipes.Dtos;

/// <summary>A menu item as presented to the client.</summary>
public sealed record MenuItemDto(
    Guid Id,
    string Name,
    string Category,
    decimal Price,
    bool IsActive,
    bool HasRecipe,
    DateTime CreatedAtUtc);