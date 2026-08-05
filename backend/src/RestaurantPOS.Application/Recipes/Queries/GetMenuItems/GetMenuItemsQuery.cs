using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Recipes.Queries.GetMenuItems;

/// <param name="Search">Matches against name or category.</param>
/// <param name="Category">Restricts to a single category when supplied.</param>
/// <param name="IsActive">Restricts to active or inactive items when supplied.</param>
public sealed record GetMenuItemsQuery(string? Search, string? Category, bool? IsActive)
    : IRequest<Result<IReadOnlyCollection<MenuItemDto>>>;

internal sealed class GetMenuItemsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetMenuItemsQuery, Result<IReadOnlyCollection<MenuItemDto>>>
{
    public async Task<Result<IReadOnlyCollection<MenuItemDto>>> Handle(
        GetMenuItemsQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.MenuItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(m => m.Name.ToLower().Contains(term) || m.Category.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(m => m.Category == request.Category);
        }

        if (request.IsActive is not null)
        {
            query = query.Where(m => m.IsActive == request.IsActive.Value);
        }

        var recipeMenuItemIds = await db.Recipes.AsNoTracking().Select(r => r.MenuItemId).ToListAsync(cancellationToken);
        var withRecipe = recipeMenuItemIds.ToHashSet();

        var items = await query
            .OrderByDescending(m => m.IsActive)
            .ThenBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<MenuItemDto> result =
        [
            .. items.Select(m => new MenuItemDto(
                m.Id, m.Name, m.Category, m.Price, m.IsActive, withRecipe.Contains(m.Id), m.CreatedAtUtc)),
        ];

        return Result.Success(result);
    }
}