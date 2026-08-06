using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Expenses.Queries.GetRecurringExpenses;

/// <summary>Every standing monthly cost the restaurant has set up.</summary>
public sealed record GetRecurringExpensesQuery : IRequest<Result<IReadOnlyCollection<RecurringExpenseDto>>>;

internal sealed class GetRecurringExpensesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetRecurringExpensesQuery, Result<IReadOnlyCollection<RecurringExpenseDto>>>
{
    public async Task<Result<IReadOnlyCollection<RecurringExpenseDto>>> Handle(
        GetRecurringExpensesQuery request, CancellationToken cancellationToken)
    {
        var recurring = await db.RecurringExpenses.AsNoTracking().ToListAsync(cancellationToken);

        var categoryNames = await db.ExpenseCategories.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var dtos = recurring
            .Select(r => r.ToDto(categoryNames.GetValueOrDefault(r.CategoryId, string.Empty)))
            .OrderBy(r => r.DayOfMonth)
            .ThenBy(r => r.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success<IReadOnlyCollection<RecurringExpenseDto>>(dtos);
    }
}
