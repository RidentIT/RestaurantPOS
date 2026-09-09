using System.Globalization;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Expenses.Queries.GetMonthlyExpenseReport;

/// <summary>
/// A month's expenses with its daily trend, weekly split, category totals, comparison against the
/// month before and any budget alerts (EXP-018, EXP-019, EXP-025, EXP-029, EXP-038).
/// </summary>
public sealed record GetMonthlyExpenseReportQuery(int Year, int Month)
    : IRequest<Result<MonthlyExpenseReportDto>>;

public sealed class GetMonthlyExpenseReportQueryValidator : AbstractValidator<GetMonthlyExpenseReportQuery>
{
    public GetMonthlyExpenseReportQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2200);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

internal sealed class GetMonthlyExpenseReportQueryHandler(IAppDbContext db)
    : IRequestHandler<GetMonthlyExpenseReportQuery, Result<MonthlyExpenseReportDto>>
{
    public async Task<Result<MonthlyExpenseReportDto>> Handle(
        GetMonthlyExpenseReportQuery request, CancellationToken cancellationToken)
    {
        var from = new DateOnly(request.Year, request.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var previousMonth = from.AddMonths(-1);
        var previousFrom = previousMonth;
        var previousTo = previousMonth.AddMonths(1).AddDays(-1);

        var categories = await db.ExpenseCategories.AsNoTracking()
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var expenses = await ExpenseAnalytics.ApprovedExpensesBetweenAsync(db, from, to, cancellationToken);
        var previousExpenses = await ExpenseAnalytics.ApprovedExpensesBetweenAsync(
            db, previousFrom, previousTo, cancellationToken);

        var revenueByDay = await ExpenseAnalytics.RevenueByDayAsync(db, from, to, cancellationToken);
        var revenue = revenueByDay.Values.Sum();
        var orderCount = await ExpenseAnalytics.OrderCountBetweenAsync(db, from, to, cancellationToken);

        var expensesByDay = expenses
            .GroupBy(e => e.ExpenseDate)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        // Every day of the month is emitted, including quiet ones, so the trend line has no gaps
        // that would make a closed Monday look like a missing reading.
        var dailyFigures = Enumerable
            .Range(0, to.Day)
            .Select(offset => from.AddDays(offset))
            .Select(day =>
            {
                var dayRevenue = revenueByDay.GetValueOrDefault(day);
                var dayExpenses = expensesByDay.GetValueOrDefault(day);

                return new DailyFigureDto(day, dayRevenue, dayExpenses, dayRevenue - dayExpenses);
            })
            .ToList();

        return Result.Success(new MonthlyExpenseReportDto(
            request.Year,
            request.Month,
            from.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
            ExpenseAnalytics.BuildProfitSummary(revenue, expenses.Sum(e => e.Amount), orderCount),
            ExpenseAnalytics.BuildCategoryBreakdown(expenses, categories),
            dailyFigures,
            BuildWeeks(dailyFigures),
            ExpenseAnalytics.BuildComparison(
                previousMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                previousExpenses,
                expenses,
                categories),
            ExpenseAnalytics.BuildBudgetAlerts(expenses, categories.Values)));
    }

    /// <summary>
    /// Splits the month into calendar weeks of seven days from the 1st. Deliberately not ISO
    /// weeks: the report is read as "the first week of January", and a week that starts on the
    /// 29th of December would confuse everyone looking at it.
    /// </summary>
    private static IReadOnlyCollection<WeeklyFigureDto> BuildWeeks(IReadOnlyCollection<DailyFigureDto> days) =>
        [.. days
            .Select((day, index) => (day, week: index / 7))
            .GroupBy(x => x.week)
            .Select(g => new WeeklyFigureDto(
                g.Key + 1,
                g.First().day.Date,
                g.Last().day.Date,
                g.Sum(x => x.day.Revenue),
                g.Sum(x => x.day.Expenses),
                g.Sum(x => x.day.Profit)))];
}
