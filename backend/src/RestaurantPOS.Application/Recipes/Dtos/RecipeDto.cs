using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Recipes.Dtos;

/// <summary>One ingredient line, with the raw material's own display details denormalised in.</summary>
public sealed record RecipeLineDto(
    Guid RawMaterialId,
    string RawMaterialName,
    UnitOfMeasurement UnitOfMeasurement,
    decimal Quantity);

public sealed record RecipeDto(
    Guid Id,
    Guid MenuItemId,
    bool IsEnabled,
    IReadOnlyCollection<RecipeLineDto> Lines,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);