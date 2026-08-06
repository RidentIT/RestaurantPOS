using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Expenses.Common;

/// <summary>
/// The arithmetic behind every expense report — one place, so the daily summary, the monthly
/// report and the analysis dashboard can never disagree about what a month cost.
/// </summary>
public static class ExpenseAnalytics
{
    /// <summary>
    /// Takings for a date range, from settled bills at the till.
    /// </summary>
    /// <remarks>
    /// Summed from recorded payments rather than from each order's total. An order's total is
    /// computed in memory from its lines, so totalling it would mean loading every bill and its
    /// items for the period; payments are stored, and settling a bill requires them to equal it
    /// exactly (BR-POS-013), so the two figures agree by construction and this one is a single
    /// query.
    /// </remarks>
    public static async Task<decimal> RevenueBetweenAsync(
        IAppDbContext db, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        return await db.OrderPayments.AsNoTracking()
            .Join(
                db.Orders.AsNoTracking().Where(o =>
                    o.Status == OrderStatus.Completed
                    && o.OrderDate != null
                    && o.OrderDate >= from
                    && o.OrderDate <= to),
                payment => payment.OrderId,
                order => order.Id,
                (payment, _) => payment.Amount)
            .SumAsync(cancellationToken);
    }

    /// <summary>Takings per business day, for a trend line.</summary>
    public static async Task<Dictionary<DateOnly, decimal>> RevenueByDayAsync(
        IAppDbContext db, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        var rows = await db.OrderPayments.AsNoTracking()
            .Join(
                db.Orders.AsNoTracking().Where(o =>
                    o.Status == OrderStatus.Completed
                    && o.OrderDate != null
                    && o.OrderDate >= from
                    && o.OrderDate <= to),
                payment => payment.OrderId,
                order => order.Id,
                (payment, order) => new { order.OrderDate, payment.Amount })
            .GroupBy(x => x.OrderDate!.Value)
            .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Date, r => r.Total);
    }

    /// <summary>
    /// Approved expenses in a range. Only approved ones are loaded anywhere reports are built,
    /// which is what enforces BR-EXP-005 and BR-EXP-010 in one place instead of at every caller.
    /// </summary>
    public static Task<List<Expense>> ApprovedExpensesBetweenAsync(
        IAppDbContext db, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        return db.Expenses.AsNoTracking()
            .Where(e => e.Status == ExpenseStatus.Approved && e.ExpenseDate >= from && e.ExpenseDate <= to)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Revenue against expenses, with the ratios an owner actually reads.</summary>
    public static ProfitSummaryDto BuildProfitSummary(decimal revenue, decimal expenses)
    {
        var profit = revenue - expenses;

        // Percentages are meaningless against no takings — a closed day is not 100% expenses,
        // it is a day with nothing to compare against. Null says that; zero would lie.
        var expenseRatio = revenue > 0 ? Round(expenses / revenue * 100m) : (decimal?)null;
        var profitMargin = revenue > 0 ? Round(profit / revenue * 100m) : (decimal?)null;

        return new ProfitSummaryDto(revenue, expenses, profit, expenseRatio, profitMargin);
    }

    /// <summary>Splits a period's spending by category, with each one's share and budget usage.</summary>
    public static IReadOnlyCollection<CategoryBreakdownDto> BuildCategoryBreakdown(
        IEnumerable<Expense> expenses, IReadOnlyDictionary<Guid, ExpenseCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(expenses);
        ArgumentNullException.ThrowIfNull(categories);

        var grouped = expenses
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(e => e.Amount), Count = g.Count() })
            .ToList();

        var overall = grouped.Sum(g => g.Total);

        return [.. grouped
            .Select(g =>
            {
                var category = categories.GetValueOrDefault(g.CategoryId);
                var budget = category?.MonthlyBudget;

                return new CategoryBreakdownDto(
                    g.CategoryId,
                    category?.Name ?? string.Empty,
                    g.Total,
                    overall > 0 ? Round(g.Total / overall * 100m) : 0m,
                    g.Count,
                    budget,
                    budget is > 0 ? Round(g.Total / budget.Value * 100m) : null);
            })
            .OrderByDescending(c => c.Total)];
    }

    /// <summary>
    /// Compares two periods and names the category that moved most (EXP-025) — the first thing
    /// an owner wants when a month costs more than the last one.
    /// </summary>
    public static PeriodComparisonDto BuildComparison(
        string previousLabel,
        IReadOnlyCollection<Expense> previous,
        IReadOnlyCollection<Expense> current,
        IReadOnlyDictionary<Guid, ExpenseCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(categories);

        var previousTotal = previous.Sum(e => e.Amount);
        var currentTotal = current.Sum(e => e.Amount);
        var change = currentTotal - previousTotal;

        var previousByCategory = previous
            .GroupBy(e => e.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var movements = current
            .GroupBy(e => e.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                Delta = g.Sum(e => e.Amount) - previousByCategory.GetValueOrDefault(g.Key),
            })
            .Where(m => m.Delta > 0)
            .OrderByDescending(m => m.Delta)
            .ToList();

        var largest = movements.FirstOrDefault();

        return new PeriodComparisonDto(
            previousLabel,
            previousTotal,
            currentTotal,
            change,
            previousTotal > 0 ? Round(change / previousTotal * 100m) : null,
            largest is null ? null : categories.GetValueOrDefault(largest.CategoryId)?.Name,
            largest?.Delta);
    }

    /// <summary>
    /// Categories at or near their monthly limit (EXP-038). Warns from 80% rather than only once
    /// the budget has been blown, since a warning that arrives after the money is spent is not a
    /// warning at all.
    /// </summary>
    public static IReadOnlyCollection<BudgetAlertDto> BuildBudgetAlerts(
        IEnumerable<Expense> monthExpenses, IEnumerable<ExpenseCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(monthExpenses);
        ArgumentNullException.ThrowIfNull(categories);

        const decimal warnAtPercentage = 80m;

        var spentByCategory = monthExpenses
            .GroupBy(e => e.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        return [.. categories
            .Where(c => c.MonthlyBudget is > 0)
            .Select(c =>
            {
                var budget = c.MonthlyBudget!.Value;
                var spent = spentByCategory.GetValueOrDefault(c.Id);
                var used = Round(spent / budget * 100m);

                return new BudgetAlertDto(
                    c.Id, c.Name, budget, spent, budget - spent, used, spent > budget);
            })
            .Where(a => a.UsedPercentage >= warnAtPercentage)
            .OrderByDescending(a => a.UsedPercentage)];
    }

    /// <summary>Money is reported to two decimals; percentages to one, which is all anyone reads.</summary>
    private static decimal Round(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
}
