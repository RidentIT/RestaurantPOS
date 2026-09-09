using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Commands.SetMenuItemActive;

/// <summary>
/// Activates or deactivates a menu item. There is no delete: past orders (once Point of Sale
/// exists) will reference this row, so history must not disappear from under them.
/// </summary>
public sealed record SetMenuItemActiveCommand(Guid MenuItemId, bool IsActive) : IRequest<Result<MenuItemDto>>;

internal sealed class SetMenuItemActiveCommandHandler(IAppDbContext db)
    : IRequestHandler<SetMenuItemActiveCommand, Result<MenuItemDto>>
{
    public async Task<Result<MenuItemDto>> Handle(SetMenuItemActiveCommand request, CancellationToken cancellationToken)
    {
        var menuItem = await db.MenuItems
            .Include(m => m.Variants)
            .FirstOrDefaultAsync(m => m.Id == request.MenuItemId, cancellationToken);

        if (menuItem is null)
        {
            return Result.Failure<MenuItemDto>(RecipeErrors.MenuItemNotFound(request.MenuItemId));
        }

        if (request.IsActive)
        {
            menuItem.Activate();
        }
        else
        {
            menuItem.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        var dto = await menuItem.ToDtoAsync(db, cancellationToken);

        return Result.Success(dto);
    }
}