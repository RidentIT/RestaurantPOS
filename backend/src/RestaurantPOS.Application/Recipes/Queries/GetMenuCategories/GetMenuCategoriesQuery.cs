using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Recipes.Queries.GetMenuCategories;

/// <summary>
/// Every category name worth offering when adding or editing a menu item — the admin-curated
/// list, plus anything already in use on a menu item that predates it.
/// </summary>
public sealed record GetMenuCategoriesQuery : IRequest<Result<IReadOnlyCollection<string>>>;

internal sealed class GetMenuCategoriesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetMenuCategoriesQuery, Result<IReadOnlyCollection<string>>>
{
    public async Task<Result<IReadOnlyCollection<string>>> Handle(
        GetMenuCategoriesQuery request, CancellationToken cancellationToken)
    {
        var registered = await db.MenuCategories.AsNoTracking()
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);

        var usedByItems = await db.MenuItems.AsNoTracking()
            .Select(m => m.Category)
            .Distinct()
            .ToListAsync(cancellationToken);

        // A registered name wins on a case-insensitive collision — it's the spelling an admin
        // deliberately chose, and an item that predates the registry doesn't get to out-vote it.
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in usedByItems)
        {
            byName.TryAdd(name, name);
        }

        foreach (var name in registered)
        {
            byName[name] = name;
        }

        IReadOnlyCollection<string> result =
            [.. byName.Values.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];

        return Result.Success(result);
    }
}
