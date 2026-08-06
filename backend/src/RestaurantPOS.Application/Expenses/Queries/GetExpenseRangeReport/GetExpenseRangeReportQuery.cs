using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Queries.GetExpenseRangeReport;

/// <summary>
/// Expenses, revenue and profit across any span of dates — what the weekly analysis dashboard and
/// the year-to-date summary both run on (EXP-030).
/// </summary>
public sealed record GetExpenseRangeReportQuery(DateOnly From, DateOnly To)
    : IRequest<Result<ExpenseRangeReportDto>>;

internal sealed class GetExpenseRangeReportQueryHandler(IAppDbContext db)
    : IRequestHandler<GetExpenseRangeReportQuery, Result<ExpenseRangeReportDto>>
{
    public async Task<Result<ExpenseRangeReportDto>> Handle(
        GetExpenseRangeReportQuery request, CancellationToken cancellationToken)
    {
        if (request.From > request.To)
        {
            return Result.Failure<ExpenseRangeReportDto>(ExpenseErrors.InvalidDateRange);
        }

        var categories = await db.ExpenseCategories.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var expenses = await ExpenseAnalytics.ApprovedExpensesBetweenAsync(
            db, request.From, request.To, cancellationToken);

        var revenueByDay = await ExpenseAnalytics.RevenueByDayAsync(
            db, request.From, request.To, cancellationToken);

        var expensesByDay = expenses
            .GroupBy(e => e.ExpenseDate)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var dayCount = request.To.DayNumber - request.From.DayNumber + 1;

        var dailyFigures = Enumerable
            .Range(0, dayCount)
            .Select(offset => request.From.AddDays(offset))
            .Select(day =>
            {
                var dayRevenue = revenueByDay.GetValueOrDefault(day);
                var dayExpenses = expensesByDay.GetValueOrDefault(day);

                return new DailyFigureDto(day, dayRevenue, dayExpenses, dayRevenue - dayExpenses);
            })
            .ToList();

        return Result.Success(new ExpenseRangeReportDto(
            request.From,
            request.To,
            ExpenseAnalytics.BuildProfitSummary(revenueByDay.Values.Sum(), expenses.Sum(e => e.Amount)),
            ExpenseAnalytics.BuildCategoryBreakdown(expenses, categories),
            dailyFigures));
    }
}
