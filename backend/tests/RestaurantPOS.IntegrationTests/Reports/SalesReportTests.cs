using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Reports;

/// <summary>
/// Sales analytics: what sold, how it was paid for, and when — plus the access rule that lets
/// Reports &amp; Analytics read the same revenue-vs-expense figures Expenses Management does.
/// </summary>
public class SalesReportTests : IntegrationTestBase
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private async Task<Guid> TableAsync(string number) =>
        (await PosApiClient.ReadAsync<TableResponse>(await Client.CreateTableAsync(number))).Id;

    private async Task<Guid> MenuItemAsync(string name, string category, decimal price) =>
        (await PosApiClient.ReadAsync<MenuItemResponse>(
            await Client.CreateMenuItemAsync(name, category, price))).Variants.Single().Id;

    /// <summary>Rings a sale through the till with a chosen quantity, payment method and discount.</summary>
    private async Task<OrderResponse> TakeSaleAsync(
        Guid tableId, Guid menuItemId, int quantity, string paymentMethod, decimal discountPercent = 0)
    {
        var order = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(tableId));
        await Client.AddOrderItemsAsync(order.Id, (menuItemId, quantity, null));
        (await Client.ConfirmOrderAsync(order.Id)).EnsureSuccessStatusCode();

        if (discountPercent > 0)
        {
            (await Client.SetOrderDiscountAsync(order.Id, "Percentage", discountPercent)).EnsureSuccessStatusCode();
        }

        (await Client.StartCheckoutAsync(order.Id)).EnsureSuccessStatusCode();

        var reloaded = await PosApiClient.ReadAsync<OrderResponse>(await Client.GetOrderAsync(order.Id));
        var paid = await Client.PayOrderAsync(order.Id, (paymentMethod, reloaded.Total, reloaded.Total));
        paid.EnsureSuccessStatusCode();

        return await PosApiClient.ReadAsync<OrderResponse>(await Client.GetOrderAsync(order.Id));
    }

    [Fact]
    public async Task TheDailyReport_NamesTheBestSellerAndSplitsByCategory()
    {
        await SignInAsAdminAsync();
        var table1 = await TableAsync("1");

        var friedRice = await MenuItemAsync("Fried Rice", "Mains", 500m);
        var lassi = await MenuItemAsync("Lassi", "Drinks", 200m);

        await TakeSaleAsync(table1, friedRice, quantity: 3, paymentMethod: "Cash");
        var table2 = await TableAsync("2");
        await TakeSaleAsync(table2, lassi, quantity: 1, paymentMethod: "Cash");

        var report = await PosApiClient.ReadAsync<SalesReportResponse>(await Client.GetDailySalesReportAsync(Today));

        report.TopItems.Should().HaveCount(2);
        report.TopItems.First().Name.Should().Be("Fried Rice", "1500 in sales beats 200");
        report.TopItems.First().QuantitySold.Should().Be(3);
        report.TopItems.First().Revenue.Should().Be(1500m);

        var mains = report.Categories.Single(c => c.Category == "Mains");
        mains.Revenue.Should().Be(1500m);
        var drinks = report.Categories.Single(c => c.Category == "Drinks");
        drinks.Revenue.Should().Be(200m);
    }

    [Fact]
    public async Task TheDailyReport_SplitsRevenueByPaymentMethod()
    {
        await SignInAsAdminAsync();
        var dish = await MenuItemAsync("Kottu", "Mains", 600m);

        await TakeSaleAsync(await TableAsync("1"), dish, 1, "Cash");
        await TakeSaleAsync(await TableAsync("2"), dish, 1, "Card");
        await TakeSaleAsync(await TableAsync("3"), dish, 1, "Card");

        var report = await PosApiClient.ReadAsync<SalesReportResponse>(await Client.GetDailySalesReportAsync(Today));

        var cash = report.PaymentMethods.Single(p => p.Method == "Cash");
        cash.Amount.Should().Be(600m);
        cash.Count.Should().Be(1);

        var card = report.PaymentMethods.Single(p => p.Method == "Card");
        card.Amount.Should().Be(1200m);
        card.Count.Should().Be(2);
        card.PercentageOfTotal.Should().BeApproximately(66.7m, 0.2m);
    }

    [Fact]
    public async Task TheDailyReport_TracksHowMuchWasDiscountedAway()
    {
        await SignInAsAdminAsync();
        var dish = await MenuItemAsync("Biryani", "Mains", 1000m);

        await TakeSaleAsync(await TableAsync("1"), dish, 1, "Cash", discountPercent: 10);
        await TakeSaleAsync(await TableAsync("2"), dish, 1, "Cash");

        var report = await PosApiClient.ReadAsync<SalesReportResponse>(await Client.GetDailySalesReportAsync(Today));

        report.Discounts.TotalOrders.Should().Be(2);
        report.Discounts.OrdersWithDiscount.Should().Be(1);
        report.Discounts.TotalDiscountGiven.Should().Be(100m);
        report.Discounts.PercentageOfOrdersDiscounted.Should().Be(50m);
    }

    [Fact]
    public async Task TheMonthlyReport_MatchesTheSumOfItsDays()
    {
        await SignInAsAdminAsync();
        var dish = await MenuItemAsync("Fried Rice", "Mains", 500m);
        await TakeSaleAsync(await TableAsync("1"), dish, 2, "Cash");

        var report = await PosApiClient.ReadAsync<SalesReportResponse>(
            await Client.GetMonthlySalesReportAsync(Today.Year, Today.Month));

        report.Summary.Revenue.Should().Be(1000m);
        report.Summary.OrderCount.Should().Be(1);
        report.TopItems.Should().ContainSingle().Which.QuantitySold.Should().Be(2);
    }

    [Fact]
    public async Task ReportsAnalyticsHoldersCanReadTheExpenseProfitReport_WithoutHoldingExpensesManagement()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "ReportsAnalytics");

        var response = await staff.GetDailyExpenseReportAsync(Today);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "the module's own catalog entry promises expense reporting, not just sales");
    }

    [Fact]
    public async Task ExpensesManagementHoldersCanStillReadTheirOwnReports_WithoutReportsAnalytics()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "ExpensesManagement");

        var response = await staff.GetMonthlyExpenseReportAsync(Today.Year, Today.Month);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SomeoneWithNeitherModuleCannotReadEitherReport()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        (await staff.GetDailyExpenseReportAsync(Today)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.GetDailySalesReportAsync(Today)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExpensesManagementAloneCannotReadTheSalesAnalyticsEndpoint()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "ExpensesManagement");

        // Sales analytics (best sellers, payment mix) is Reports & Analytics' own territory —
        // unlike the shared P&L figures, holding Expenses Management alone is not enough.
        var response = await staff.GetDailySalesReportAsync(Today);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
