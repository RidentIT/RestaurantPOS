using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by menu item and recipe use cases.</summary>
public static class RecipeErrors
{
    public static Error MenuItemNotFound(Guid id) =>
        Error.NotFound("MenuItem.NotFound", $"No menu item was found with id '{id}'.");

    public static Error VariantNotFound(Guid variantId) =>
        Error.NotFound("MenuItem.VariantNotFound", $"No menu item size was found with id '{variantId}'.");

    public static readonly Error MenuItemNameTaken =
        Error.Conflict("MenuItem.NameTaken", "A menu item with that name already exists.");

    public static readonly Error CategoryNameTaken =
        Error.Conflict("MenuCategory.NameTaken", "That category already exists.");

    public static Error CategoryNotFound(string name) =>
        Error.NotFound("MenuCategory.NotFound", $"No category was found named '{name}'.");

    public static Error CategoryInUse(int menuItemCount) =>
        Error.Conflict(
            "MenuCategory.InUse",
            menuItemCount == 1
                ? "1 menu item still uses this category."
                : $"{menuItemCount} menu items still use this category.");

    public static Error VariantHasOrderHistory(int variantCount) =>
        Error.Conflict(
            "MenuItem.VariantInUse",
            variantCount == 1
                ? "One of the sizes you're removing has already been sold and can't be deleted."
                : $"{variantCount} of the sizes you're removing have already been sold and can't be deleted.");

    public static Error RecipeNotFound(Guid menuItemVariantId) =>
        Error.NotFound("Recipe.NotFound", $"Menu item size '{menuItemVariantId}' does not have a recipe.");

    public static readonly Error EmptyRecipe =
        Error.Validation("Recipe.Empty", "A recipe must contain at least one raw material.");

    public static readonly Error DuplicateRawMaterial =
        Error.Validation("Recipe.DuplicateRawMaterial", "A raw material cannot appear more than once in the same recipe.");

    public static readonly Error UnknownRawMaterial =
        Error.Validation("Recipe.UnknownRawMaterial", "One or more selected raw materials could not be found.");

    public static readonly Error InactiveRawMaterial =
        Error.Validation("Recipe.InactiveRawMaterial", "One or more selected raw materials are inactive.");
}