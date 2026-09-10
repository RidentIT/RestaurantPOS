using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Orders;

/// <summary>
/// Covers stewards end to end: the roster an administrator maintains, crediting a table order to
/// one, and the sales-by-steward figures the owner awards on.
/// </summary>
public class StewardSalesTests : IntegrationTestBase
{
    private async Task<Guid> CreateTableAsync(string number)
    {
        var response = await Client.CreateTableAsync(number);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await PosApiClient.ReadAsync<TableResponse>(response)).Id;
    }

    private async Task<Guid> CreateMenuItemAsync(string name, decimal price)
    {
        var response = await Client.CreateMenuItemAsync(name, "Mains", price);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await PosApiClient.ReadAsync<MenuItemResponse>(response)).Variants.Single().Id;
    }

    private async Task<Guid> CreateStewardAsync(string name)
    {
        var response = await Client.CreateStewardAsync(name);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await PosApiClient.ReadAsync<StewardResponse>(response)).Id;
    }

    /// <summary>Rings a table order right through to a completed, paid bill credited to a steward.</summary>
    private async Task SettleOrderAsync(Guid tableId, Guid? stewardId, Guid variantId, int quantity, decimal unitPrice)
    {
        var order = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(tableId, stewardId));
        await Client.AddOrderItemsAsync(order.Id, (variantId, quantity, null));
        (await Client.ConfirmOrderAsync(order.Id)).EnsureSuccessStatusCode();
        (await Client.StartCheckoutAsync(order.Id)).EnsureSuccessStatusCode();
        var total = unitPrice * quantity;
        (await Client.PayOrderAsync(order.Id, ("Cash", total, total))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Steward_CanBeCreatedRenamedAndRetired()
    {
        await SignInAsAdminAsync();

        var id = await CreateStewardAsync("Kamal");
        (await Client.CreateStewardAsync("kamal")).StatusCode.Should().Be(
            HttpStatusCode.Conflict, "steward names are unique regardless of case");

        (await Client.RenameStewardAsync(id, "Kamal Perera")).EnsureSuccessStatusCode();
        (await Client.SetStewardActiveAsync(id, false)).EnsureSuccessStatusCode();

        var all = await PosApiClient.ReadAsync<List<StewardResponse>>(await Client.GetStewardsAsync());
        all.Single().Should().Match<StewardResponse>(s => s.Name == "Kamal Perera" && !s.IsActive);

        var active = await PosApiClient.ReadAsync<List<StewardResponse>>(await Client.GetStewardsAsync(isActive: true));
        active.Should().BeEmpty("a retired steward drops out of the order picker");
    }

    [Fact]
    public async Task AssigningARetiredSteward_IsRejected()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("1");
        var stewardId = await CreateStewardAsync("Nimal");
        (await Client.SetStewardActiveAsync(stewardId, false)).EnsureSuccessStatusCode();

        var created = await Client.CreateOrderAsync(tableId, stewardId);
        created.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(created)).Should().Be("Steward.Inactive");
    }

    [Fact]
    public async Task OpeningATableOrderWithASteward_CreditsThemOnTheOrderTileAndKot()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("4");
        var dish = await CreateMenuItemAsync("Kottu", 900m);
        var stewardId = await CreateStewardAsync("Sunil");

        var order = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(tableId, stewardId));
        order.StewardId.Should().Be(stewardId);
        order.StewardName.Should().Be("Sunil");

        await Client.AddOrderItemsAsync(order.Id, (dish, 1, null));
        var confirmed = await PosApiClient.ReadAsync<OrderMutationResponse>(await Client.ConfirmOrderAsync(order.Id));
        confirmed.Kot!.StewardName.Should().Be("Sunil", "the kitchen slip names the steward serving the table");

        var tables = await PosApiClient.ReadAsync<List<TableResponse>>(await Client.GetTablesAsync());
        tables.Single(t => t.Id == tableId).CurrentOrder!.StewardName.Should().Be("Sunil");
    }

    [Fact]
    public async Task AssigningAStewardToALiveOrder_UpdatesIt_AndCanBeCleared()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("5");
        var first = await CreateStewardAsync("Amara");
        var second = await CreateStewardAsync("Bandara");

        var order = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(tableId));
        order.StewardName.Should().BeNull("no steward was picked when the table was opened");

        var reassigned = await PosApiClient.ReadAsync<OrderResponse>(
            await Client.AssignOrderStewardAsync(order.Id, first));
        reassigned.StewardName.Should().Be("Amara");

        (await PosApiClient.ReadAsync<OrderResponse>(await Client.AssignOrderStewardAsync(order.Id, second)))
            .StewardName.Should().Be("Bandara");

        (await PosApiClient.ReadAsync<OrderResponse>(await Client.AssignOrderStewardAsync(order.Id, null)))
            .StewardName.Should().BeNull("passing a null id clears the steward");
    }

    [Fact]
    public async Task ATakeawayOrder_CannotBeGivenASteward()
    {
        await SignInAsAdminAsync();
        var stewardId = await CreateStewardAsync("Chandana");

        var takeaway = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(tableId: null, stewardId));
        takeaway.StewardId.Should().BeNull("a takeaway order silently carries no steward");

        var rejected = await Client.AssignOrderStewardAsync(takeaway.Id, stewardId);
        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(rejected)).Should().Be("Steward.NotOnTableOrder");
    }

    [Fact]
    public async Task SalesBySteward_RanksNamedStewardsByNetSalesWithUnassignedLast()
    {
        await SignInAsAdminAsync();
        var t1 = await CreateTableAsync("1");
        var t2 = await CreateTableAsync("2");
        var t3 = await CreateTableAsync("3");
        var dish = await CreateMenuItemAsync("Rice & Curry", 500m);
        var top = await CreateStewardAsync("Kamal");
        var other = await CreateStewardAsync("Nimal");

        await SettleOrderAsync(t1, top, dish, quantity: 3, unitPrice: 500m);    // net 1500
        await SettleOrderAsync(t2, other, dish, quantity: 1, unitPrice: 500m);  // net 500
        await SettleOrderAsync(t3, stewardId: null, dish, quantity: 2, unitPrice: 500m); // unassigned, net 1000

        var report = await PosApiClient.ReadAsync<SalesReportResponse>(
            await Client.GetDailySalesReportAsync(DateOnly.FromDateTime(DateTime.Now)));

        report.StewardSales.Should().HaveCount(3);

        var best = report.StewardSales.First();
        best.StewardName.Should().Be("Kamal", "highest net sales is first — the steward to award");
        best.NetSales.Should().Be(1500m);
        best.OrdersServed.Should().Be(1);
        best.ItemsSold.Should().Be(3);

        report.StewardSales.ElementAt(1).StewardName.Should().Be("Nimal");
        report.StewardSales.Last().Should().Match<SalesByStewardResponse>(
            s => s.StewardId == null && s.StewardName == "Unassigned" && s.NetSales == 1000m);
    }

    [Fact]
    public async Task Cashier_CanListStewardsButNotCreateThem()
    {
        await SignInAsAdminAsync();
        await CreateStewardAsync("Kamal");
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        (await staff.GetStewardsAsync(isActive: true)).EnsureSuccessStatusCode();
        (await staff.CreateStewardAsync("Nimal")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
