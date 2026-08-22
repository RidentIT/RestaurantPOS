using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Expenses.Queries.GetExpenseCategories;

/// <summary>Every expense category, built-in and custom (EXP-010).</summary>
public sealed record GetExpenseCategoriesQuery(bool? IsActive)
    : IRequest<Result<IReadOnlyCollection<ExpenseCategoryDto>>>;

internal sealed class GetExpenseCategoriesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetExpenseCategoriesQuery, Result<IReadOnlyCollection<ExpenseCategoryDto>>>
{
    public async Task<Result<IReadOnlyCollection<ExpenseCategoryDto>>> Handle(
        GetExpenseCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = db.ExpenseCategories.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == request.IsActive.Value);
        }

        var categories = await query.ToListAsync(cancellationToken);

        var names = categories.ToDictionary(c => c.Id, c => c.Name);

        var counts = await db.Expenses.AsNoTracking()
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CategoryId, g => g.Count, cancellationToken);

        var dtos = categories
            .Select(c => c.ToDto(
                c.ParentCategoryId is null ? null : names.GetValueOrDefault(c.ParentCategoryId.Value),
                counts.GetValueOrDefault(c.Id)))
            // Parents first with their children beneath, so the list reads as the tree it is.
            .OrderBy(c => c.ParentCategoryName ?? c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.ParentCategoryName is null ? 0 : 1)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success<IReadOnlyCollection<ExpenseCategoryDto>>(dtos);
    }
}
