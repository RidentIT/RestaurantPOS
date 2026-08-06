using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Orders;

/// <summary>The kitchen display: the ticket queue, its statuses, and how those reach the till.</summary>
public class KitchenOperationsTests : IntegrationTestBase
{
    private async Task<(Guid TableId, Guid MenuItemId)> SeedAsync(string tableNumber = "2")
    {
        var table = await PosApiClient.ReadAsync<TableResponse>(await Client.CreateTableAsync(tableNumber));
        var menuItem = await PosApiClient.ReadAsync<MenuItemResponse>(
            await Client.CreateMenuItemAsync("Fried Rice", "Mains", 250m));

        return (table.Id, menuItem.Id);
    }

    private async Task<OrderResponse> OpenOrderAsync(Guid tableId, Guid menuItemId, int quantity = 1)
    {
        var order = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(tableId));
        await Client.AddOrderItemsAsync(order.Id, (menuItemId, quantity, null));
        var confirmed = await Client.ConfirmOrderAsync(order.Id);

        return (await PosApiClient.ReadAsync<OrderMutationResponse>(confirmed)).Order;
    }

    [Fact]
    public async Task AConfirmedOrder_AppearsOnTheKitchenDisplayAsANewTicket()
    {
        await SignInAsAdminAsync();
        var (tableId, menuItemId) = await SeedAsync();
        await OpenOrderAsync(tableId, menuItemId);

        var tickets = await PosApiClient.ReadAsync<List<KitchenTicketResponse>>(
            await Client.GetKitchenTicketsAsync());

        tickets.Should().ContainSingle();
        tickets[0].Status.Should().Be("New");
        tickets[0].Kind.Should().Be("New");
        tickets[0].TableNumber.Should().Be("2");
        tickets[0].Lines.Single().MenuItemName.Should().Be("Fried Rice");
    }

    [Fact]
    public async Task ATicketMovesThroughPreparingReadyAndServed()
    {
        await SignInAsAdminAsync();
        var (tableId, menuItemId) = await SeedAsync();
        await OpenOrderAsync(tableId, menuItemId);
        var ticket = (await PosApiClient.ReadAsync<List<KitchenTicketResponse>>(
            await Client.GetKitchenTicketsAsync())).Single();

        foreach (var status in new[] { "Preparing", "Ready", "Served" })
        {
            var advanced = await Client.AdvanceKitchenTicketAsync(ticket.Id, status);
            advanced.EnsureSuccessStatusCode();
            (await PosApiClient.ReadAsync<KitchenTicketResponse>(advanced)).Status.Should().Be(status);
        }

        var queue = await PosApiClient.ReadAsync<List<KitchenTicketResponse>>(
            await Client.GetKitchenTicketsAsync());

        queue.Should().BeEmpty("a served ticket drops off the work queue");
    }

    [Fact]
    public async Task ATicketCannotBeMovedBackwards()
    {
        await SignInAsAdminAsync();
        var (tableId, menuItemId) = await SeedAsync();
        await OpenOrderAsync(tableId, menuItemId);
        var ticket = (await PosApiClient.ReadAsync<List<KitchenTicketResponse>>(
            await Client.GetKitchenTicketsAsync())).Single();

        await Client.AdvanceKitchenTicketAsync(ticket.Id, "Ready");
        var backwards = await Client.AdvanceKitchenTicketAsync(ticket.Id, "Preparing");

        backwards.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(backwards)).Should().Be("KitchenTicket.CannotGoBack");
    }

    [Fact]
    public async Task TheTillSeesTheLeastAdvancedTicketForATable()
    {
        await SignInAsAdminAsync();
        var (tableId, menuItemId) = await SeedAsync();
        var order = await OpenOrderAsync(tableId, menuItemId);

        var firstTicket = (await PosApiClient.ReadAsync<List<KitchenTicketResponse>>(
            await Client.GetKitchenTicketsAsync())).Single();
        await Client.AdvanceKitchenTicketAsync(firstTicket.Id, "Ready");

        // A second round is ordered, so the table is not ready after all.
        await Client.AddOrderItemsAsync(order.Id, (menuItemId, 1, null));

        var tables = await PosApiClient.ReadAsync<List<TableResponse>>(await Client.GetTablesAsync());

        tables.Single(t => t.Id == tableId).CurrentOrder!.KitchenStatus
            .Should().Be("New", "one dish is plated but another has only just been ordered");
    }

    [Fact]
    public async Task ReprintingATicket_ReproducesItAndCountsThePrint()
    {
        await SignInAsAdminAsync();
        var (tableId, menuItemId) = await SeedAsync();
        await OpenOrderAsync(tableId, menuItemId, quantity: 2);
        var ticket = (await PosApiClient.ReadAsync<List<KitchenTicketResponse>>(
            await Client.GetKitchenTicketsAsync())).Single();

        var reprint = await Client.ReprintKitchenTicketAsync(ticket.Id);

        reprint.EnsureSuccessStatusCode();
        var slip = await PosApiClient.ReadAsync<KotDocumentResponse>(reprint);
        slip.TicketId.Should().Be(ticket.Id);
        slip.PrintCount.Should().Be(2);
        slip.Lines.Single().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task AVoidedItemPutsACancellationSlipOnTheDisplay()
    {
        await SignInAsAdminAsync();
        await Client.SetApprovalPinAsync(AdminPassword, "4417");
        var (tableId, menuItemId) = await SeedAsync();
        var order = await OpenOrderAsync(tableId, menuItemId);

        await Client.VoidOrderItemAsync(order.Id, order.Items.Single().Id, "4417");

        var tickets = await PosApiClient.ReadAsync<List<KitchenTicketResponse>>(
            await Client.GetKitchenTicketsAsync());

        tickets.Should().HaveCount(2);
        tickets.Should().ContainSingle(t => t.Kind == "Cancellation")
            .Which.Lines.Single().Note.Should().Be("CANCELLED");
    }

    [Fact]
    public async Task StaffWithoutKitchenOperations_CannotReachTheDisplay()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        (await staff.GetKitchenTicketsAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
