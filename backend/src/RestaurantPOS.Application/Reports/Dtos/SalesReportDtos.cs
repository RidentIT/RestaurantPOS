using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Reports.Dtos;

/// <summary>One menu item size's contribution to a period's sales, best sellers first.</summary>
public sealed record TopMenuItemDto(
    Guid MenuItemVariantId, string Name, string Category, int QuantitySold, decimal Revenue);

/// <summary>One menu category's share of a period's sales.</summary>
public sealed record CategorySalesDto(
    string Category, decimal Revenue, decimal PercentageOfTotal, int QuantitySold);

/// <summary>How customers actually paid — cash still dominant, or has card overtaken it.</summary>
public sealed record PaymentMethodBreakdownDto(
    OrderPaymentMethod Method, decimal Amount, decimal PercentageOfTotal, int Count);

/// <summary>Takings by hour of day, in the restaurant's own local time — where the rush actually is.</summary>
public sealed record HourlySalesDto(int Hour, decimal Revenue, int OrderCount);

/// <summary>Takings by day of the week, aggregated across the whole period.</summary>
public sealed record DayOfWeekSalesDto(DayOfWeek Day, decimal Revenue, int OrderCount);

/// <summary>
/// One steward's share of a period's sales, so the owner can see who served the most and award
/// them. Takeaway orders and any dine-in order without a steward fall into a single "Unassigned"
/// row (<see cref="StewardId"/> null) so the numbers still reconcile to the period total.
/// </summary>
public sealed record SalesByStewardDto(
    Guid? StewardId,
    string StewardName,
    int OrdersServed,
    int ItemsSold,
    /// <summary>Menu value of everything sold, before any discount.</summary>
    decimal GrossSales,
    decimal DiscountsGiven,
    /// <summary>Gross less discounts — the figure the table is ranked on.</summary>
    decimal NetSales,
    /// <summary>Net sales divided by orders served. Zero when the steward served none.</summary>
    decimal AverageBill);

/// <summary>How much of the period's sales were discounted, and how often.</summary>
public sealed record DiscountSummaryDto(
    decimal TotalDiscountGiven,
    int OrdersWithDiscount,
    int TotalOrders,
    /// <summary>Share of orders that carried a discount, 0-100.</summary>
    decimal PercentageOfOrdersDiscounted);

/// <summary>
/// A period's sales in depth — what the daily/monthly expense report doesn't cover: what sold,
/// how it was paid for, and when the till was actually busy.
/// </summary>
public sealed record SalesReportDto(
    DateOnly From,
    DateOnly To,
    string PeriodLabel,
    ProfitSummaryDto Summary,
    IReadOnlyCollection<TopMenuItemDto> TopItems,
    IReadOnlyCollection<CategorySalesDto> Categories,
    IReadOnlyCollection<PaymentMethodBreakdownDto> PaymentMethods,
    IReadOnlyCollection<HourlySalesDto> HourlyPattern,
    IReadOnlyCollection<DayOfWeekSalesDto> DayOfWeekPattern,
    /// <summary>Sales per steward, highest net first — the first named steward is the period's best.</summary>
    IReadOnlyCollection<SalesByStewardDto> StewardSales,
    DiscountSummaryDto Discounts);
