namespace RestaurantPOS.API.Contracts.Recipes;

public sealed record CreateMenuItemRequest(string Name, string Category, decimal Price);

public sealed record UpdateMenuItemRequest(string Name, string Category, decimal Price);

public sealed record SetMenuItemActiveRequest(bool IsActive);