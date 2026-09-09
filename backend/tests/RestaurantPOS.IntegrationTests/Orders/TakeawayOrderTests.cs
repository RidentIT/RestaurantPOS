using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Orders;

/// <summary>
/// A takeaway order runs through the exact same till flow as a dine-in one — confirm, print a
/// KOT, take payment — the only difference is it never holds a table (POS-031 does not apply).
/// </summary>
public class TakeawayOrderTests : IntegrationTestBase
{
    private async Task<Guid> CreateMenuItemAsync(string name, decimal price)
    {
        var response = await Client.CreateMenuItemAsync(name, "Mains", price);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var item = await PosApiClient.ReadAsync<MenuItemResponse>(response);
        return item.Variants.Single().Id;
    }

    [Fact]
    public async Task CreatingATakeawayOrder_HoldsNoTable()
    {
        await SignInAsAdminAsync();

        var created = await Client.CreateOrderAsync(tableId: null);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await PosApiClient.ReadAsync<OrderResponse>(created);
        order.TableId.Should().BeNull();
        order.TableNumber.Should().BeNull();
    }

    [Fact]
    public async Task ATakeawayOrder_ConfirmsPrintsAKotAndCanBePaid()
    {
        await SignInAsAdminAsync();
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);

        var created = await Client.CreateOrderAsync(tableId: null);
        var order = await PosApiClient.ReadAsync<OrderResponse>(created);

        await Client.AddOrderItemsAsync(order.Id, (friedRice, 1, null));
        var confirmed = await Client.ConfirmOrderAsync(order.Id);
        confirmed.EnsureSuccessStatusCode();
        var result = await PosApiClient.ReadAsync<OrderMutationResponse>(confirmed);

        result.Order.Status.Should().Be("Open");
        result.Order.TableNumber.Should().BeNull();
        result.Kot.Should().NotBeNull();
        result.Kot!.TableNumber.Should().BeNull("a takeaway KOT has no table to print");

        await Client.StartCheckoutAsync(order.Id);
        var paid = await Client.PayOrderAsync(order.Id, ("Cash", 250m, 250m));

        paid.EnsureSuccessStatusCode();
        var receipt = await PosApiClient.ReadAsync<ReceiptDocumentResponse>(paid);
        receipt.TableNumber.Should().BeNull();
    }

    [Fact]
    public async Task SeveralTakeawayOrders_CanBeOpenAtOnceWithoutConflicting()
    {
        await SignInAsAdminAsync();

        var first = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(tableId: null));
        var second = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(tableId: null));

        // Unlike a table, nothing about a takeaway order stops a second (or third, or fourth) one
        // from being opened at the same time — there's no seat being double-booked.
        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public async Task TheFloorPlan_NeverListsATakeawayOrder()
    {
        await SignInAsAdminAsync();
        var table = await PosApiClient.ReadAsync<TableResponse>(await Client.CreateTableAsync("9"));
        await Client.CreateOrderAsync(tableId: null);

        var tables = await PosApiClient.ReadAsync<List<TableResponse>>(await Client.GetTablesAsync());

        tables.Single(t => t.Id == table.Id).CurrentOrder.Should().BeNull(
            "a takeaway order never occupies a table, so the one just opened must not appear on it");
    }

    [Fact]
    public async Task GetOrders_CanBeFilteredToTakeawayOnly()
    {
        await SignInAsAdminAsync();
        var tableId = (await PosApiClient.ReadAsync<TableResponse>(await Client.CreateTableAsync("9"))).Id;
        await Client.CreateOrderAsync(tableId);
        await Client.CreateOrderAsync(tableId: null);

        var takeawayOnly = await PosApiClient.ReadAsync<List<OrderSummaryResponse>>(
            await Client.GetOrdersAsync("?openOnly=true&isTakeaway=true"));

        takeawayOnly.Should().ContainSingle().Which.TableNumber.Should().BeNull();
    }
}
