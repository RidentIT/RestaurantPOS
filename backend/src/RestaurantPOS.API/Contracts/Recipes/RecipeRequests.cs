namespace RestaurantPOS.API.Contracts.Recipes;

public sealed record RecipeLineRequest(Guid RawMaterialId, decimal Quantity);

/// <summary>Creates the menu item's recipe if it has none, or replaces its lines if it does.</summary>
public sealed record UpsertRecipeRequest(IReadOnlyCollection<RecipeLineRequest> Lines);

public sealed record SetRecipeEnabledRequest(bool IsEnabled);