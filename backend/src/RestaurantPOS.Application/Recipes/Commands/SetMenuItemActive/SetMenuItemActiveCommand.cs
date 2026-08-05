using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
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
        var menuItem = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == request.MenuItemId, cancellationToken);

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

        var hasRecipe = await db.Recipes.AnyAsync(r => r.MenuItemId == menuItem.Id, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new MenuItemDto(
            menuItem.Id, menuItem.Name, menuItem.Category, menuItem.Price, menuItem.IsActive,
            hasRecipe, menuItem.CreatedAtUtc));
    }
}