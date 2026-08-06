using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by raw material, supplier and stock movement use cases.</summary>
public static class InventoryErrors
{
    public static Error RawMaterialNotFound(Guid id) =>
        Error.NotFound("RawMaterial.NotFound", $"No raw material was found with id '{id}'.");

    public static readonly Error RawMaterialNameTaken =
        Error.Conflict("RawMaterial.NameTaken", "A raw material with that name already exists.");

    public static readonly Error RawMaterialInactive =
        Error.Validation("RawMaterial.Inactive", "This raw material is inactive and cannot be used in a new transaction.");

    public static readonly Error EmptyLines =
        Error.Validation("Inventory.EmptyLines", "At least one raw material line is required.");

    public static readonly Error DuplicateRawMaterialLine =
        Error.Validation("Inventory.DuplicateRawMaterialLine", "A raw material cannot appear more than once in the same transaction.");

    public static readonly Error InsufficientStock =
        Error.Conflict(
            "Inventory.InsufficientStock",
            "This would take a raw material's stock below zero. Check the quantities and current stock levels.");

    public static Error MenuItemHasNoRecipe(Guid menuItemId) =>
        Error.Conflict(
            "Inventory.MenuItemHasNoRecipe",
            $"Menu item '{menuItemId}' has no recipe, so no stock was deducted.");

    public static Error GoodsReceivedNoteNotFound(Guid id) =>
        Error.NotFound("GoodsReceivedNote.NotFound", $"No GRN was found with id '{id}'.");

    public static Error StockReleaseNotFound(Guid id) =>
        Error.NotFound("StockRelease.NotFound", $"No stock release was found with id '{id}'.");

    public static readonly Error RecipeDisabled =
        Error.Conflict("Inventory.RecipeDisabled", "This menu item's recipe is disabled and cannot be used for a new sale.");
}