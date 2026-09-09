using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
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
            .Include(m => m.Variants)
            .FirstOrDefaultAsync(m => m.Id == request.MenuItemId, cancellationToken);

        if (menuItem is null)
        {
            return Result.Failure<MenuItemDto>(RecipeErrors.MenuItemNotFound(request.MenuItemId));
        }

        var dto = await menuItem.ToDtoAsync(db, cancellationToken);

        return Result.Success(dto);
    }
}