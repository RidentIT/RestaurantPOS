using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Expenses;

/// <summary>
/// Expense reporting, including the figures that pull revenue across from the till, and the
/// recurring costs that materialise themselves.
/// </summary>
public class ExpenseReportTests : IntegrationTestBase
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private async Task<Guid> CategoryIdAsync(string name)
    {
        var categories = await PosApiClient.ReadAsync<List<ExpenseCategoryResponse>>(
            await Client.GetExpenseCategoriesAsync());

        return categories.Single(c => c.Name == name).Id;
    }

    private async Task<Guid> RecordApprovedAsync(string categoryName, decimal amount, DateOnly? date = null)
    {
        var created = await Client.CreateExpenseAsync(
            date ?? Today, await CategoryIdAsync(categoryName), amount, $"{categoryName} spend");

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var expense = await PosApiClient.ReadAsync<ExpenseResponse>(created);

        (await Client.ApproveExpensesAsync(null, expense.Id)).EnsureSuccessStatusCode();

        return expense.Id;
    }

    /// <summary>Rings a sale through the till so the reports have real revenue to work against.</summary>
    private async Task TakeSaleAsync(string tableNumber, decimal price)
    {
        var table = await PosApiClient.ReadAsync<TableResponse>(await Client.CreateTableAsync(tableNumber));
        var menuItem = await PosApiClient.ReadAsync<MenuItemResponse>(
            await Client.CreateMenuItemAsync($"Dish {tableNumber}", "Mains", price));

        var order = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(table.Id));
        await Client.AddOrderItemsAsync(order.Id, (menuItem.Id, 1, null));
        (await Client.ConfirmOrderAsync(order.Id)).EnsureSuccessStatusCode();
        (await Client.StartCheckoutAsync(order.Id)).EnsureSuccessStatusCode();
        (await Client.PayOrderAsync(order.Id, ("Cash", price, price))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task TheDailyReport_BreaksSpendingDownByCategory()
    {
        await SignInAsAdminAsync();
        await RecordApprovedAsync("Gas/LPG", 2500m);
        await RecordApprovedAsync("Electricity", 3500m);
        await RecordApprovedAsync("Water/Waste", 1200m);
        await RecordApprovedAsync("Staff Meals", 800m);
        await RecordApprovedAsync("Miscellaneous", 500m);

        var report = await PosApiClient.ReadAsync<DailyExpenseReportResponse>(
            await Client.GetDailyExpenseReportAsync(Today));

        report.Summary.Expenses.Should().Be(8500m);
        report.Categories.Should().HaveCount(5);

        var electricity = report.Categories.Single(c => c.CategoryName == "Electricity");
        electricity.Total.Should().Be(3500m);
        electricity.PercentageOfTotal.Should().BeApproximately(41.2m, 0.2m);

        report.HighestCategory.Should().Be("Electricity");
        report.LowestCategory.Should().Be("Miscellaneous");
    }

    [Fact]
    public async Task TheDailyReport_CalculatesProfitAgainstTakings()
    {
        await SignInAsAdminAsync();
        await TakeSaleAsync("1", 30_000m);
        await TakeSaleAsync("2", 15_000m);
        await RecordApprovedAsync("Gas/LPG", 8500m);

        var report = await PosApiClient.ReadAsync<DailyExpenseReportResponse>(
            await Client.GetDailyExpenseReportAsync(Today));

        report.Summary.Revenue.Should().Be(45_000m, "revenue comes from settled bills at the till");
        report.Summary.Expenses.Should().Be(8500m);
        report.Summary.Profit.Should().Be(36_500m);
        report.Summary.ProfitMargin.Should().BeApproximately(81.1m, 0.2m);
        report.Summary.ExpenseRatio.Should().BeApproximately(18.9m, 0.2m);
    }

    [Fact]
    public async Task OnlyApprovedExpensesReachTheReports()
    {
        await SignInAsAdminAsync();
        await RecordApprovedAsync("Gas/LPG", 2500m);

        // A draft and a rejected expense must both be invisible to the totals.
        var draft = await PosApiClient.ReadAsync<ExpenseResponse>(
            await Client.CreateExpenseAsync(Today, await CategoryIdAsync("Electricity"), 9999m));

        var rejected = await PosApiClient.ReadAsync<ExpenseResponse>(
            await Client.CreateExpenseAsync(Today, await CategoryIdAsync("Water/Waste"), 7777m));
        (await Client.RejectExpensesAsync("No", rejected.Id)).EnsureSuccessStatusCode();

        var report = await PosApiClient.ReadAsync<DailyExpenseReportResponse>(
            await Client.GetDailyExpenseReportAsync(Today));

        report.Summary.Expenses.Should().Be(2500m, "only approved expenses count (BR-EXP-005)");
        report.Categories.Should().ContainSingle();

        // The listing still shows everything, so a manager sees what is waiting on them.
        report.Expenses.Should().HaveCount(3);
        report.Expenses.Should().Contain(e => e.Id == draft.Id);
    }

    [Fact]
    public async Task ProfitPercentagesAreNullOnADayWithNoTakings()
    {
        await SignInAsAdminAsync();
        await RecordApprovedAsync("Gas/LPG", 2500m);

        var report = await PosApiClient.ReadAsync<DailyExpenseReportResponse>(
            await Client.GetDailyExpenseReportAsync(Today));

        report.Summary.Revenue.Should().Be(0m);
        report.Summary.Profit.Should().Be(-2500m);
        report.Summary.ProfitMargin.Should().BeNull("a closed day has nothing to take a percentage of");
        report.Summary.ExpenseRatio.Should().BeNull();
    }

    [Fact]
    public async Task TheDailyReport_ComparesWithYesterday()
    {
        await SignInAsAdminAsync();
        await RecordApprovedAsync("Gas/LPG", 8200m, Today.AddDays(-1));
        await RecordApprovedAsync("Gas/LPG", 8500m);

        var report = await PosApiClient.ReadAsync<DailyExpenseReportResponse>(
            await Client.GetDailyExpenseReportAsync(Today));

        report.Comparison.PreviousTotal.Should().Be(8200m);
        report.Comparison.CurrentTotal.Should().Be(8500m);
        report.Comparison.Change.Should().Be(300m);
        report.Comparison.ChangePercentage.Should().BeApproximately(3.7m, 0.1m);
    }

    [Fact]
    public async Task TheMonthlyReport_TotalsCategoriesAndSplitsIntoWeeks()
    {
        await SignInAsAdminAsync();

        // Last month, so every date used is safely in the past whenever this runs — an expense
        // dated in the future is rejected, and today may well be the 2nd.
        var firstOfLastMonth = new DateOnly(Today.Year, Today.Month, 1).AddMonths(-1);

        await RecordApprovedAsync("Gas/LPG", 25_000m, firstOfLastMonth);
        await RecordApprovedAsync("Electricity", 32_500m, firstOfLastMonth.AddDays(9));

        var report = await PosApiClient.ReadAsync<MonthlyExpenseReportResponse>(
            await Client.GetMonthlyExpenseReportAsync(firstOfLastMonth.Year, firstOfLastMonth.Month));

        report.Summary.Expenses.Should().Be(57_500m);
        report.Categories.Should().HaveCount(2);
        report.DailyFigures.Should().HaveCount(
            DateTime.DaysInMonth(firstOfLastMonth.Year, firstOfLastMonth.Month),
            "every day appears so the trend line has no gaps");

        report.WeeklyFigures.Should().NotBeEmpty();
        report.WeeklyFigures.First().Expenses.Should().Be(25_000m, "the 1st falls in week one");
        report.WeeklyFigures.Skip(1).First().Expenses.Should().Be(32_500m, "the 10th falls in week two");
    }

    [Fact]
    public async Task TheMonthlyReport_NamesTheCategoryThatRoseMost()
    {
        await SignInAsAdminAsync();
        var thisMonth = new DateOnly(Today.Year, Today.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);

        await RecordApprovedAsync("Miscellaneous", 5000m, lastMonth);
        await RecordApprovedAsync("Gas/LPG", 20_000m, lastMonth);
        await RecordApprovedAsync("Miscellaneous", 11_200m, thisMonth);
        await RecordApprovedAsync("Gas/LPG", 21_000m, thisMonth);

        var report = await PosApiClient.ReadAsync<MonthlyExpenseReportResponse>(
            await Client.GetMonthlyExpenseReportAsync(Today.Year, Today.Month));

        report.Comparison.PreviousTotal.Should().Be(25_000m);
        report.Comparison.CurrentTotal.Should().Be(32_200m);
        report.Comparison.LargestIncreaseCategory.Should().Be("Miscellaneous");
        report.Comparison.LargestIncreaseAmount.Should().Be(6200m);
    }

    [Fact]
    public async Task BudgetAlerts_FireWhenACategoryNearsAndPassesItsLimit()
    {
        await SignInAsAdminAsync();
        var thisMonth = new DateOnly(Today.Year, Today.Month, 1);

        // Miscellaneous is budgeted at 50,000 by default; go over it.
        await RecordApprovedAsync("Miscellaneous", 52_300m, thisMonth);
        // Electricity is budgeted at 35,000; sit just under at 93%.
        await RecordApprovedAsync("Electricity", 32_500m, thisMonth);
        // Gas is budgeted at 30,000; a small spend should stay quiet.
        await RecordApprovedAsync("Gas/LPG", 1000m, thisMonth);

        var report = await PosApiClient.ReadAsync<MonthlyExpenseReportResponse>(
            await Client.GetMonthlyExpenseReportAsync(Today.Year, Today.Month));

        var over = report.BudgetAlerts.Single(a => a.CategoryName == "Miscellaneous");
        over.IsOverBudget.Should().BeTrue();
        over.SpentThisMonth.Should().Be(52_300m);
        over.Remaining.Should().Be(-2300m);

        var near = report.BudgetAlerts.Single(a => a.CategoryName == "Electricity");
        near.IsOverBudget.Should().BeFalse();
        near.UsedPercentage.Should().BeApproximately(92.9m, 0.2m);

        report.BudgetAlerts.Should().NotContain(a => a.CategoryName == "Gas/LPG",
            "a category well inside its budget is not worth interrupting anyone about");
    }

    [Fact]
    public async Task TheRangeReport_CoversAnyStretchOfDays()
    {
        await SignInAsAdminAsync();
        await RecordApprovedAsync("Gas/LPG", 1000m, Today.AddDays(-2));
        await RecordApprovedAsync("Gas/LPG", 2000m, Today);
        await RecordApprovedAsync("Gas/LPG", 4000m, Today.AddDays(-10));

        var response = await Client.GetExpenseRangeReportAsync(Today.AddDays(-3), Today);
        response.EnsureSuccessStatusCode();

        var report = await PosApiClient.ReadAsync<MonthlyExpenseReportResponse>(response);
        report.Summary.Expenses.Should().Be(3000m, "the expense ten days ago falls outside the range");
        report.DailyFigures.Should().HaveCount(4);
    }

    [Fact]
    public async Task ARangeEndingBeforeItStarts_IsRejected()
    {
        await SignInAsAdminAsync();

        var response = await Client.GetExpenseRangeReportAsync(Today, Today.AddDays(-5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Expense.InvalidDateRange");
    }

    [Fact]
    public async Task RecurringExpenses_AppearOnceForTheMonthHoweverOftenGenerationRuns()
    {
        await SignInAsAdminAsync();
        var rentCategory = await CategoryIdAsync("Rent/Lease");

        var created = await Client.CreateRecurringExpenseAsync(
            rentCategory, 50_000m, "Monthly rent", "BankTransfer", 1);
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var first = await Client.GenerateRecurringExpensesAsync();
        first.EnsureSuccessStatusCode();
        (await PosApiClient.ReadAsync<int>(first)).Should().Be(1);

        // Called again, as it would be every time the screen is opened.
        var second = await Client.GenerateRecurringExpensesAsync();
        second.EnsureSuccessStatusCode();
        (await PosApiClient.ReadAsync<int>(second)).Should().Be(0, "opening the screen twice must not create two rents");

        var expenses = await PosApiClient.ReadAsync<List<ExpenseSummaryResponse>>(
            await Client.GetExpensesAsync("?search=Monthly rent"));

        expenses.Should().ContainSingle();
        expenses[0].IsRecurring.Should().BeTrue();
        expenses[0].Status.Should().Be("Draft", "a generated expense is reviewed before it counts");
        expenses[0].Amount.Should().Be(50_000m);
    }

    [Fact]
    public async Task AStoppedRecurringExpense_GeneratesNothing()
    {
        await SignInAsAdminAsync();
        var recurring = await PosApiClient.ReadAsync<RecurringExpenseResponse>(
            await Client.CreateRecurringExpenseAsync(
                await CategoryIdAsync("Salaries"), 180_000m, "Staff salaries", "BankTransfer", 5));

        (await Client.SetRecurringExpenseActiveAsync(recurring.Id, false)).EnsureSuccessStatusCode();

        var generated = await Client.GenerateRecurringExpensesAsync();

        generated.EnsureSuccessStatusCode();
        (await PosApiClient.ReadAsync<int>(generated)).Should().Be(0);
    }
}
