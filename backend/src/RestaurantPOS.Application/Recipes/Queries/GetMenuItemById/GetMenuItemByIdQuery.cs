using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Queries.GetMenuItemById;

public sealed record GetMenuItemByIdQuery(Guid MenuItemId) : IRequest<Result<MenuItemDto>>;

internal sealed class GetMenuItemByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetMenuItemByIdQuery, Result<MenuItemDto>>
{
    public async Task<Result<MenuItemDto>> Handle(GetMenuItemByIdQuery request, CancellationToken cancellationToken)
    {
        var menuItem = await db.MenuItems.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MenuItemId, cancellationToken);

        if (menuItem is null)
        {
            return Result.Failure<MenuItemDto>(RecipeErrors.MenuItemNotFound(request.MenuItemId));
        }

        var hasRecipe = await db.Recipes.AsNoTracking().AnyAsync(r => r.MenuItemId == menuItem.Id, cancellationToken);

        return Result.Success(new MenuItemDto(
            menuItem.Id, menuItem.Name, menuItem.Category, menuItem.Price, menuItem.IsActive,
            hasRecipe, menuItem.CreatedAtUtc));
    }
}