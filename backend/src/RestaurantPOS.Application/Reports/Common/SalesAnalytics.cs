using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Reports.Dtos;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Reports.Common;

/// <summary>
/// The arithmetic behind a sales report — what sold, how it was paid for, and when the till was
/// actually busy. One place, shared by the daily and monthly reports, the same way
/// <see cref="ExpenseAnalytics"/> keeps every expense report agreeing on what a month cost.
/// </summary>
public static class SalesAnalytics
{
    public static async Task<SalesReportDto> BuildAsync(
        IAppDbContext db, DateOnly from, DateOnly to, string periodLabel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        // Only what a sales report needs — not the kitchen tickets or receipt an order screen
        // would load, which would cost real rows over a month of trading for nothing this uses.
        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Where(o => o.Status == OrderStatus.Completed && o.OrderDate != null
                && o.OrderDate >= from && o.OrderDate <= to)
            .ToListAsync(cancellationToken);

        // A recipe/price belongs to the variant that was actually sold, but its category lives on
        // the parent menu item — so both are resolved and reduced to one variant-id-keyed lookup.
        var variantIds = orders.SelectMany(o => o.ActiveItems).Select(i => i.MenuItemVariantId).Distinct().ToList();
        var variants = await db.MenuItemVariants.AsNoTracking()
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        var parentIds = variants.Values.Select(v => v.MenuItemId).Distinct().ToList();
        var categoriesByMenuItem = await db.MenuItems.AsNoTracking()
            .Where(m => parentIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Category, cancellationToken);

        var categoryByVariant = variants.ToDictionary(
            kv => kv.Key,
            kv => categoriesByMenuItem.GetValueOrDefault(kv.Value.MenuItemId, string.Empty));

        var stewardIds = orders.Where(o => o.StewardId.HasValue).Select(o => o.StewardId!.Value).Distinct().ToList();
        var stewardNames = await db.Stewards.AsNoTracking()
            .Where(s => stewardIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var revenue = orders.Sum(o => o.Total);
        var expenses = (await ExpenseAnalytics.ApprovedExpensesBetweenAsync(db, from, to, cancellationToken))
            .Sum(e => e.Amount);
        var summary = ExpenseAnalytics.BuildProfitSummary(revenue, expenses, orders.Count);

        return new SalesReportDto(
            from,
            to,
            periodLabel,
            summary,
            BuildTopItems(orders, categoryByVariant),
            BuildCategorySales(orders, categoryByVariant),
            BuildPaymentMethods(orders),
            BuildHourlyPattern(orders),
            BuildDayOfWeekPattern(orders),
            BuildStewardSales(orders, stewardNames),
            BuildDiscountSummary(orders));
    }

    private static IReadOnlyCollection<SalesByStewardDto> BuildStewardSales(
        IEnumerable<Order> orders, IReadOnlyDictionary<Guid, string> stewardNames)
    {
        return [.. orders
            .GroupBy(o => o.StewardId)
            .Select(g =>
            {
                var gross = g.Sum(o => o.Subtotal);
                var discounts = g.Sum(o => o.DiscountAmount);
                var net = gross - discounts;
                var count = g.Count();

                return new SalesByStewardDto(
                    g.Key,
                    g.Key is { } id ? stewardNames.GetValueOrDefault(id, "Unknown") : "Unassigned",
                    count,
                    g.Sum(o => o.ActiveItems.Sum(i => i.Quantity)),
                    Money(gross),
                    Money(discounts),
                    Money(net),
                    count > 0 ? Money(net / count) : 0m);
            })
            // Named stewards ranked by net sales; the "Unassigned" bucket always sits last.
            .OrderBy(s => s.StewardId is null)
            .ThenByDescending(s => s.NetSales)];
    }

    private static IReadOnlyCollection<TopMenuItemDto> BuildTopItems(
        IEnumerable<Order> orders, IReadOnlyDictionary<Guid, string> categoryByVariant)
    {
        return [.. orders
            .SelectMany(o => o.ActiveItems)
            .GroupBy(i => i.MenuItemVariantId)
            .Select(g => new TopMenuItemDto(
                g.Key,
                g.First().MenuItemName,
                categoryByVariant.GetValueOrDefault(g.Key, string.Empty),
                g.Sum(i => i.Quantity),
                g.Sum(i => i.LineTotal)))
            .OrderByDescending(i => i.Revenue)];
    }

    private static IReadOnlyCollection<CategorySalesDto> BuildCategorySales(
        IEnumerable<Order> orders, IReadOnlyDictionary<Guid, string> categoryByVariant)
    {
        var items = orders.SelectMany(o => o.ActiveItems).ToList();

        var grouped = items
            .GroupBy(i => categoryByVariant.TryGetValue(i.MenuItemVariantId, out var category) ? category : "Uncategorised")
            .Select(g => new { Category = g.Key, Revenue = g.Sum(i => i.LineTotal), Quantity = g.Sum(i => i.Quantity) })
            .ToList();

        var total = grouped.Sum(g => g.Revenue);

        return [.. grouped
            .Select(g => new CategorySalesDto(
                g.Category,
                g.Revenue,
                total > 0 ? Round(g.Revenue / total * 100m) : 0m,
                g.Quantity))
            .OrderByDescending(c => c.Revenue)];
    }

    private static IReadOnlyCollection<PaymentMethodBreakdownDto> BuildPaymentMethods(IEnumerable<Order> orders)
    {
        var payments = orders.SelectMany(o => o.Payments).ToList();
        var total = payments.Sum(p => p.Amount);

        return [.. payments
            .GroupBy(p => p.Method)
            .Select(g => new PaymentMethodBreakdownDto(
                g.Key,
                g.Sum(p => p.Amount),
                total > 0 ? Round(g.Sum(p => p.Amount) / total * 100m) : 0m,
                g.Count()))
            .OrderByDescending(p => p.Amount)];
    }

    /// <summary>
    /// Takings by hour, in local time — an order's <c>CompletedAtUtc</c> is always a true UTC
    /// instant regardless of what <see cref="DateTime.Kind"/> survives the round trip through
    /// SQLite, and <see cref="DateTime.ToLocalTime"/> treats an <c>Unspecified</c> kind as UTC, so
    /// this converts correctly either way.
    /// </summary>
    private static IReadOnlyCollection<HourlySalesDto> BuildHourlyPattern(IEnumerable<Order> orders)
    {
        var completed = orders.Where(o => o.CompletedAtUtc is not null).ToList();

        var byHour = completed
            .GroupBy(o => o.CompletedAtUtc!.Value.ToLocalTime().Hour)
            .ToDictionary(g => g.Key, g => (Revenue: g.Sum(o => o.Total), Count: g.Count()));

        return [.. Enumerable.Range(0, 24)
            .Select(hour =>
            {
                var (revenue, count) = byHour.GetValueOrDefault(hour);
                return new HourlySalesDto(hour, revenue, count);
            })];
    }

    private static IReadOnlyCollection<DayOfWeekSalesDto> BuildDayOfWeekPattern(IEnumerable<Order> orders)
    {
        var completed = orders.Where(o => o.CompletedAtUtc is not null).ToList();

        var byDay = completed
            .GroupBy(o => o.CompletedAtUtc!.Value.ToLocalTime().DayOfWeek)
            .ToDictionary(g => g.Key, g => (Revenue: g.Sum(o => o.Total), Count: g.Count()));

        return [.. Enum.GetValues<DayOfWeek>()
            .Select(day =>
            {
                var (revenue, count) = byDay.GetValueOrDefault(day);
                return new DayOfWeekSalesDto(day, revenue, count);
            })];
    }

    private static DiscountSummaryDto BuildDiscountSummary(IReadOnlyCollection<Order> orders)
    {
        var withDiscount = orders.Where(o => o.DiscountAmount > 0).ToList();

        return new DiscountSummaryDto(
            withDiscount.Sum(o => o.DiscountAmount),
            withDiscount.Count,
            orders.Count,
            orders.Count > 0 ? Round((decimal)withDiscount.Count / orders.Count * 100m) : 0m);
    }

    private static decimal Round(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
