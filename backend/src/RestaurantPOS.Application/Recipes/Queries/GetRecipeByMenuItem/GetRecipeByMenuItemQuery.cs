using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Queries.GetRecipeByMenuItem;

/// <summary>
/// Displays every ingredient associated with a menu item (REC-011). Not every menu item has a
/// recipe, so a missing one is reported clearly rather than treated as an error.
/// </summary>
public sealed record GetRecipeByMenuItemQuery(Guid MenuItemVariantId) : IRequest<Result<RecipeDto?>>;

internal sealed class GetRecipeByMenuItemQueryHandler(IAppDbContext db)
    : IRequestHandler<GetRecipeByMenuItemQuery, Result<RecipeDto?>>
{
    public async Task<Result<RecipeDto?>> Handle(GetRecipeByMenuItemQuery request, CancellationToken cancellationToken)
    {
        var variantExists = await db.MenuItemVariants.AnyAsync(v => v.Id == request.MenuItemVariantId, cancellationToken);
        if (!variantExists)
        {
            return Result.Failure<RecipeDto?>(RecipeErrors.VariantNotFound(request.MenuItemVariantId));
        }

        var recipe = await db.Recipes.AsNoTracking()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.MenuItemVariantId == request.MenuItemVariantId, cancellationToken);

        return Result.Success(recipe is null ? null : await recipe.ToDtoAsync(db, cancellationToken));
    }
}