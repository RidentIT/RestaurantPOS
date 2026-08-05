using MediatR;

using RestaurantPOS.API.Contracts.Recipes;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Recipes.Commands.CreateMenuItem;
using RestaurantPOS.Application.Recipes.Commands.DeleteRecipe;
using RestaurantPOS.Application.Recipes.Commands.SetMenuItemActive;
using RestaurantPOS.Application.Recipes.Commands.SetRecipeEnabled;
using RestaurantPOS.Application.Recipes.Commands.UpdateMenuItem;
using RestaurantPOS.Application.Recipes.Commands.UpsertRecipe;
using RestaurantPOS.Application.Recipes.Queries.GetMenuItemById;
using RestaurantPOS.Application.Recipes.Queries.GetMenuItems;
using RestaurantPOS.Application.Recipes.Queries.GetRecipeByMenuItem;
using RestaurantPOS.Domain.Enums;

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

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMenuItemByIdQuery(id), ct);
                return result.ToHttpResult();
            })
            .WithName("GetMenuItemById")
            .WithSummary("Loads a single menu item.");

        group.MapPost("/", async (CreateMenuItemRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateMenuItemCommand(request.Name, request.Category, request.Price);
                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(item => $"/api/v1/menu-items/{item.Id}");
            })
            .WithName("CreateMenuItem")
            .WithSummary("Creates a menu item.");

        group.MapPut("/{id:guid}", async (
                Guid id, UpdateMenuItemRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateMenuItemCommand(id, request.Name, request.Category, request.Price);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdateMenuItem")
            .WithSummary("Updates a menu item's name, category and price.");

        group.MapPut("/{id:guid}/status", async (
                Guid id, SetMenuItemActiveRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetMenuItemActiveCommand(id, request.IsActive), ct);
                return result.ToHttpResult();
            })
            .WithName("SetMenuItemActive")
            .WithSummary("Activates or deactivates a menu item.");

        group.MapGet("/{id:guid}/recipe", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetRecipeByMenuItemQuery(id), ct);
                return result.ToHttpResult();
            })
            .WithName("GetRecipeByMenuItem")
            .WithSummary("Displays every ingredient associated with a menu item, or null if it has no recipe.");

        group.MapPut("/{id:guid}/recipe", async (
                Guid id, UpsertRecipeRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpsertRecipeCommand(
                    id, [.. request.Lines.Select(l => new RecipeLineInput(l.RawMaterialId, l.Quantity))]);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpsertRecipe")
            .WithSummary("Creates the menu item's recipe, or replaces its lines if one already exists.");

        group.MapPut("/{id:guid}/recipe/status", async (
                Guid id, SetRecipeEnabledRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetRecipeEnabledCommand(id, request.IsEnabled), ct);
                return result.ToHttpResult();
            })
            .WithName("SetRecipeEnabled")
            .WithSummary("Enables or disables a recipe. A disabled recipe cannot be used for a new sale.");

        group.MapDelete("/{id:guid}/recipe", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new DeleteRecipeCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("DeleteRecipe")
            .WithSummary("Removes a menu item's recipe. The change is kept in the audit log.");

        return routes;
    }
}