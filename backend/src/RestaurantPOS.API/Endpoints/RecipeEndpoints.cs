using MediatR;

using RestaurantPOS.API.Contracts.Recipes;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Recipes.Commands.CreateMenuCategory;
using RestaurantPOS.Application.Recipes.Commands.CreateMenuItem;
using RestaurantPOS.Application.Recipes.Commands.DeleteMenuCategory;
using RestaurantPOS.Application.Recipes.Commands.DeleteRecipe;
using RestaurantPOS.Application.Recipes.Commands.SetMenuItemActive;
using RestaurantPOS.Application.Recipes.Commands.SetRecipeEnabled;
using RestaurantPOS.Application.Recipes.Commands.UpdateMenuItem;
using RestaurantPOS.Application.Recipes.Commands.UpsertRecipe;
using RestaurantPOS.Application.Recipes.Queries.GetMenuCategories;
using RestaurantPOS.Application.Recipes.Queries.GetMenuItemById;
using RestaurantPOS.Application.Recipes.Queries.GetMenuItems;
using RestaurantPOS.Application.Recipes.Queries.GetRecipeByMenuItem;
using RestaurantPOS.Domain.Enums;

// Create's and Update's per-command variant input records share a name — aliased so each call
// site below can stay unqualified rather than spelling out the full namespace every time.
using NewVariant = RestaurantPOS.Application.Recipes.Commands.CreateMenuItem.MenuItemVariantInput;
using EditedVariant = RestaurantPOS.Application.Recipes.Commands.UpdateMenuItem.MenuItemVariantInput;

namespace RestaurantPOS.API.Endpoints;

/// <summary>Menu items and the recipe (bill of materials) attached to each one.</summary>
public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var group = routes.MapGroup("/menu-items")
            .WithTags("Recipes")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.RecipeManagement));

        group.MapGet("/", async (
                string? search, string? category, bool? isActive, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMenuItemsQuery(search, category, isActive), ct);
                return result.ToHttpResult();
            })
            .WithName("GetMenuItems")
            .WithSummary("Lists menu items, optionally filtered.");

        group.MapGet("/categories", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMenuCategoriesQuery(), ct);
                return result.ToHttpResult();
            })
            .WithName("GetMenuCategories")
            .WithSummary("Every category name worth offering when adding or editing a menu item.");

        group.MapPost("/categories", async (
                CreateMenuCategoryRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CreateMenuCategoryCommand(request.Name), ct);
                return result.ToHttpResult();
            })
            .WithName("CreateMenuCategory")
            .WithSummary("Registers a category name, whether or not a menu item uses it yet.");

        group.MapDelete("/categories", async (string name, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new DeleteMenuCategoryCommand(name), ct);
                return result.ToHttpResult();
            })
            .WithName("DeleteMenuCategory")
            .WithSummary("Removes a category, refused while any menu item still carries it.");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMenuItemByIdQuery(id), ct);
                return result.ToHttpResult();
            })
            .WithName("GetMenuItemById")
            .WithSummary("Loads a single menu item.");

        group.MapPost("/", async (CreateMenuItemRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateMenuItemCommand(
                    request.Name,
                    request.Category,
                    // A missing "variants" array deserializes to null rather than an empty list —
                    // treated the same so the validator's "add at least one size" rule reports it
                    // cleanly instead of a NullReferenceException here.
                    [.. (request.Variants ?? []).Select(v => new NewVariant(v.Name, v.Price))]);
                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(item => $"/api/v1/menu-items/{item.Id}");
            })
            .WithName("CreateMenuItem")
            .WithSummary("Creates a menu item with its sizes.");

        group.MapPut("/{id:guid}", async (
                Guid id, UpdateMenuItemRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateMenuItemCommand(
                    id,
                    request.Name,
                    request.Category,
                    [.. (request.Variants ?? []).Select(v => new EditedVariant(v.Id, v.Name, v.Price))]);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdateMenuItem")
            .WithSummary("Updates a menu item's name, category and sizes.");

        group.MapPut("/{id:guid}/status", async (
                Guid id, SetMenuItemActiveRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetMenuItemActiveCommand(id, request.IsActive), ct);
                return result.ToHttpResult();
            })
            .WithName("SetMenuItemActive")
            .WithSummary("Activates or deactivates a menu item.");

        // Each size gets its own independent recipe (see MenuItemVariant), so these routes hang off
        // the variant id rather than the parent menu item's. The literal "variants" segment keeps
        // them from colliding with "/menu-items/{id:guid}" above.
        group.MapGet("/variants/{variantId:guid}/recipe", async (Guid variantId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetRecipeByMenuItemQuery(variantId), ct);
                return result.ToHttpResult();
            })
            .WithName("GetRecipeByMenuItemVariant")
            .WithSummary("Displays every ingredient associated with a menu item size, or null if it has no recipe.");

        group.MapPut("/variants/{variantId:guid}/recipe", async (
                Guid variantId, UpsertRecipeRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpsertRecipeCommand(
                    variantId, [.. request.Lines.Select(l => new RecipeLineInput(l.RawMaterialId, l.Quantity))]);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpsertRecipe")
            .WithSummary("Creates the size's recipe, or replaces its lines if one already exists.");

        group.MapPut("/variants/{variantId:guid}/recipe/status", async (
                Guid variantId, SetRecipeEnabledRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetRecipeEnabledCommand(variantId, request.IsEnabled), ct);
                return result.ToHttpResult();
            })
            .WithName("SetRecipeEnabled")
            .WithSummary("Enables or disables a recipe. A disabled recipe cannot be used for a new sale.");

        group.MapDelete("/variants/{variantId:guid}/recipe", async (Guid variantId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new DeleteRecipeCommand(variantId), ct);
                return result.ToHttpResult();
            })
            .WithName("DeleteRecipe")
            .WithSummary("Removes a menu item size's recipe. The change is kept in the audit log.");

        return routes;
    }
}