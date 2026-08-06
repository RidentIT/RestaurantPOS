using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Expenses.Queries.GetDailyExpenseReport;

/// <summary>
/// One day's expenses against that day's takings (EXP-017, EXP-026, EXP-028), with yesterday for
/// comparison.
/// </summary>
public sealed record GetDailyExpenseReportQuery(DateOnly Date) : IRequest<Result<DailyExpenseReportDto>>;

internal sealed class GetDailyExpenseReportQueryHandler(IAppDbContext db)
    : IRequestHandler<GetDailyExpenseReportQuery, Result<DailyExpenseReportDto>>
{
    public async Task<Result<DailyExpenseReportDto>> Handle(
        GetDailyExpenseReportQuery request, CancellationToken cancellationToken)
    {
        var date = request.Date;
        var previousDate = date.AddDays(-1);

        var categories = await db.ExpenseCategories.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var todaysExpenses = await ExpenseAnalytics.ApprovedExpensesBetweenAsync(db, date, date, cancellationToken);
        var yesterdaysExpenses = await ExpenseAnalytics.ApprovedExpensesBetweenAsync(
            db, previousDate, previousDate, cancellationToken);

        var revenue = await ExpenseAnalytics.RevenueBetweenAsync(db, date, date, cancellationToken);
        var summary = ExpenseAnalytics.BuildProfitSummary(revenue, todaysExpenses.Sum(e => e.Amount));

        var breakdown = ExpenseAnalytics.BuildCategoryBreakdown(todaysExpenses, categories);
        var comparison = ExpenseAnalytics.BuildComparison(
            previousDate.ToString("yyyy-MM-dd"), yesterdaysExpenses, todaysExpenses, categories);

        var userNames = await db.Users.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        // The listing shows the whole day, not only what was approved: a manager opening the
        // daily report wants to see the two bills still waiting on them, not a total that
        // quietly excludes them.
        var allOfToday = await ExpenseRepository.WithAggregate(db.Expenses.AsNoTracking())
            .Where(e => e.ExpenseDate == date)
            .ToListAsync(cancellationToken);

        var listed = allOfToday
            .OrderByDescending(e => e.Number)
            .Select(e => e.ToSummaryDto(
                categories.GetValueOrDefault(e.CategoryId)?.Name ?? string.Empty,
                userNames.GetValueOrDefault(e.RecordedByUserId, string.Empty)))
            .ToList();

        return Result.Success(new DailyExpenseReportDto(
            date,
            summary,
            breakdown,
            comparison,
            listed,
            breakdown.FirstOrDefault()?.CategoryName,
            breakdown.LastOrDefault()?.CategoryName));
    }
}
