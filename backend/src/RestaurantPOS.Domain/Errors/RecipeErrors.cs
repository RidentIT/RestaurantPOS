using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by menu item and recipe use cases.</summary>
public static class RecipeErrors
{
    public static Error MenuItemNotFound(Guid id) =>
        Error.NotFound("MenuItem.NotFound", $"No menu item was found with id '{id}'.");

    public static readonly Error MenuItemNameTaken =
        Error.Conflict("MenuItem.NameTaken", "A menu item with that name already exists.");

    public static Error RecipeNotFound(Guid menuItemId) =>
        Error.NotFound("Recipe.NotFound", $"Menu item '{menuItemId}' does not have a recipe.");

    public static readonly Error EmptyRecipe =
        Error.Validation("Recipe.Empty", "A recipe must contain at least one raw material.");

    public static readonly Error DuplicateRawMaterial =
        Error.Validation("Recipe.DuplicateRawMaterial", "A raw material cannot appear more than once in the same recipe.");

    public static readonly Error UnknownRawMaterial =
        Error.Validation("Recipe.UnknownRawMaterial", "One or more selected raw materials could not be found.");

    public static readonly Error InactiveRawMaterial =
        Error.Validation("Recipe.InactiveRawMaterial", "One or more selected raw materials are inactive.");
}