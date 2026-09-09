namespace RestaurantPOS.API.Contracts.Recipes;

/// <summary>One size to create the item with — no id, since creation never references an existing size.</summary>
public sealed record CreateMenuItemVariantRequest(string? Name, decimal Price);

public sealed record CreateMenuItemRequest(
    string Name, string Category, IReadOnlyCollection<CreateMenuItemVariantRequest> Variants);

/// <summary>
/// One size after the edit — an existing size to update when <see cref="Id"/> is supplied, a new
/// one to add when it is null. A size missing from the submitted list is removed.
/// </summary>
public sealed record UpdateMenuItemVariantRequest(Guid? Id, string? Name, decimal Price);

public sealed record UpdateMenuItemRequest(
    string Name, string Category, IReadOnlyCollection<UpdateMenuItemVariantRequest> Variants);

public sealed record SetMenuItemActiveRequest(bool IsActive);

public sealed record CreateMenuCategoryRequest(string Name);
