using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Queries.GetExpenses;

/// <summary>
/// The expense list with every filter the screens offer (EXP-020 to EXP-024).
/// </summary>
/// <param name="Search">Matches description, reference or expense number — however it is remembered.</param>
public sealed record GetExpensesQuery(
    DateOnly? From,
    DateOnly? To,
    Guid? CategoryId,
    ExpensePaymentMethod? PaymentMethod,
    ExpenseStatus? Status,
    bool? IsPaid,
    string? Search) : IRequest<Result<IReadOnlyCollection<ExpenseSummaryDto>>>;

internal sealed class GetExpensesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetExpensesQuery, Result<IReadOnlyCollection<ExpenseSummaryDto>>>
{
    public async Task<Result<IReadOnlyCollection<ExpenseSummaryDto>>> Handle(
        GetExpensesQuery request, CancellationToken cancellationToken)
    {
        if (request.From is { } from && request.To is { } to && from > to)
        {
            return Result.Failure<IReadOnlyCollection<ExpenseSummaryDto>>(ExpenseErrors.InvalidDateRange);
        }

        var query = ExpenseRepository.WithAggregate(db.Expenses.AsNoTracking());

        if (request.From is { } start)
        {
            query = query.Where(e => e.ExpenseDate >= start);
        }

        if (request.To is { } end)
        {
            query = query.Where(e => e.ExpenseDate <= end);
        }

        if (request.CategoryId is { } categoryId)
        {
            // A parent category includes whatever hangs beneath it, so filtering by "Utilities"
            // does not hide the electricity bills filed under it.
            var childIds = await db.ExpenseCategories.AsNoTracking()
                .Where(c => c.ParentCategoryId == categoryId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            childIds.Add(categoryId);
            query = query.Where(e => childIds.Contains(e.CategoryId));
        }

        if (request.PaymentMethod is { } method)
        {
            query = query.Where(e => e.PaymentMethod == method);
        }

        if (request.Status is { } status)
        {
            query = query.Where(e => e.Status == status);
        }

        if (request.IsPaid is { } isPaid)
        {
            query = query.Where(e => e.IsPaid == isPaid);
        }

        var expenses = await query.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();

            expenses = [.. expenses.Where(e =>
                (e.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (e.PaymentReference?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || e.ExpenseNumber.Contains(term, StringComparison.OrdinalIgnoreCase))];
        }

        var categoryNames = await db.ExpenseCategories.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var userNames = await db.Users.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var dtos = expenses
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Number)
            .Select(e => e.ToSummaryDto(
                categoryNames.GetValueOrDefault(e.CategoryId, string.Empty),
                userNames.GetValueOrDefault(e.RecordedByUserId, string.Empty)))
            .ToList();

        return Result.Success<IReadOnlyCollection<ExpenseSummaryDto>>(dtos);
    }
}
