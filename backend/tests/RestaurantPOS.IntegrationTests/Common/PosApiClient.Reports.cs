namespace RestaurantPOS.IntegrationTests.Common;

public sealed record TopMenuItemResponse(
    Guid MenuItemId, string Name, string Category, int QuantitySold, decimal Revenue);

public sealed record CategorySalesResponse(
    string Category, decimal Revenue, decimal PercentageOfTotal, int QuantitySold);

public sealed record PaymentMethodBreakdownResponse(
    string Method, decimal Amount, decimal PercentageOfTotal, int Count);

public sealed record HourlySalesResponse(int Hour, decimal Revenue, int OrderCount);

public sealed record DayOfWeekSalesResponse(string Day, decimal Revenue, int OrderCount);

public sealed record DiscountSummaryResponse(
    decimal TotalDiscountGiven, int OrdersWithDiscount, int TotalOrders, decimal PercentageOfOrdersDiscounted);

public sealed record SalesReportResponse(
    DateOnly From,
    DateOnly To,
    string PeriodLabel,
    ProfitSummaryResponse Summary,
    IReadOnlyCollection<TopMenuItemResponse> TopItems,
    IReadOnlyCollection<CategorySalesResponse> Categories,
    IReadOnlyCollection<PaymentMethodBreakdownResponse> PaymentMethods,
    IReadOnlyCollection<HourlySalesResponse> HourlyPattern,
    IReadOnlyCollection<DayOfWeekSalesResponse> DayOfWeekPattern,
    DiscountSummaryResponse Discounts);

public sealed partial class PosApiClient
{
    public Task<HttpResponseMessage> GetDailySalesReportAsync(DateOnly date) =>
        Http.GetAsync($"{BaseUrl}/reports/sales/daily?date={date:yyyy-MM-dd}");

    public Task<HttpResponseMessage> GetMonthlySalesReportAsync(int year, int month) =>
        Http.GetAsync($"{BaseUrl}/reports/sales/monthly?year={year}&month={month}");
}
