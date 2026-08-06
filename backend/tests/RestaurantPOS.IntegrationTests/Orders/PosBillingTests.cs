using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Orders;

/// <summary>
/// Covers the till end to end, including the two-table scenario the requirements describe: two
/// bills open at once, items added to each after confirmation, then each settled on its own.
/// </summary>
public class PosBillingTests : IntegrationTestBase
{
    private const string Pin = "4417";

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

        return (await PosApiClient.ReadAsync<MenuItemResponse>(response)).Id;
    }

    /// <summary>Sets the administrator's approval PIN so PIN-gated actions can be exercised.</summary>
    private async Task ConfigurePinAsync()
    {
        var response = await Client.SetApprovalPinAsync(AdminPassword, Pin);
        response.EnsureSuccessStatusCode();
    }

    private async Task<OrderResponse> OpenOrderAsync(Guid tableId, params (Guid Id, int Qty)[] items)
    {
        var created = await Client.CreateOrderAsync(tableId);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await PosApiClient.ReadAsync<OrderResponse>(created);

        var added = await Client.AddOrderItemsAsync(
            order.Id, [.. items.Select(i => (i.Id, i.Qty, (string?)null))]);
        added.EnsureSuccessStatusCode();

        var confirmed = await Client.ConfirmOrderAsync(order.Id);
        confirmed.EnsureSuccessStatusCode();

        return (await PosApiClient.ReadAsync<OrderMutationResponse>(confirmed)).Order;
    }

    [Fact]
    public async Task ConfirmingAnOrder_NumbersItPrintsAKotAndOccupiesTheTable()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);

        var created = await Client.CreateOrderAsync(tableId);
        var order = await PosApiClient.ReadAsync<OrderResponse>(created);
        order.Status.Should().Be("Draft");
        order.OrderNumber.Should().BeNull();

        await Client.AddOrderItemsAsync(order.Id, (friedRice, 1, null));

        var confirmed = await Client.ConfirmOrderAsync(order.Id);
        confirmed.EnsureSuccessStatusCode();
        var result = await PosApiClient.ReadAsync<OrderMutationResponse>(confirmed);

        result.Order.Status.Should().Be("Open");
        result.Order.OrderNumber.Should().Be(1);
        result.Order.Total.Should().Be(250m);

        result.Kot.Should().NotBeNull("confirming an order must produce a slip for the kitchen");
        result.Kot!.Kind.Should().Be("New");
        result.Kot.TableNumber.Should().Be("2");
        result.Kot.Lines.Should().ContainSingle().Which.MenuItemName.Should().Be("Fried Rice");

        var tables = await PosApiClient.ReadAsync<List<TableResponse>>(await Client.GetTablesAsync());
        tables.Single(t => t.Id == tableId).CurrentOrder!.Status.Should().Be("Open");
    }

    [Fact]
    public async Task ConfirmingAnOrderWithNoItems_IsRejected()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");

        var created = await Client.CreateOrderAsync(tableId);
        var order = await PosApiClient.ReadAsync<OrderResponse>(created);

        var confirmed = await Client.ConfirmOrderAsync(order.Id);

        confirmed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(confirmed)).Should().Be("Order.NoItems");
    }

    [Fact]
    public async Task ATableCannotHoldTwoOrdersAtOnce()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        await OpenOrderAsync(tableId, (friedRice, 1));

        var second = await Client.CreateOrderAsync(tableId);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(second)).Should().Be("Table.Occupied");
    }

    [Fact]
    public async Task AddingItemsToAConfirmedOrder_NeedsNoPinAndPrintsAnotherKot()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var sandwich = await CreateMenuItemAsync("Sandwich", 150m);

        var order = await OpenOrderAsync(tableId, (friedRice, 1));

        var added = await Client.AddOrderItemsAsync(order.Id, (sandwich, 1, null));

        added.EnsureSuccessStatusCode();
        var result = await PosApiClient.ReadAsync<OrderMutationResponse>(added);

        result.Order.Total.Should().Be(400m);
        result.Order.Status.Should().Be("Open");
        result.Kot!.Kind.Should().Be("Addition");
        result.Kot.TicketNumber.Should().Be(2);
        result.Kot.Lines.Should().ContainSingle()
            .Which.MenuItemName.Should().Be("Sandwich", "the kitchen is already cooking the rice");
    }

    [Fact]
    public async Task ChangingAQuantityOnAnOpenOrder_RequiresTheApprovalPin()
    {
        await SignInAsAdminAsync();
        await ConfigurePinAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var order = await OpenOrderAsync(tableId, (friedRice, 1));
        var itemId = order.Items.Single().Id;

        var withoutPin = await Client.ChangeOrderItemQuantityAsync(order.Id, itemId, 3);
        withoutPin.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var wrongPin = await Client.ChangeOrderItemQuantityAsync(order.Id, itemId, 3, "0000");
        wrongPin.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var withPin = await Client.ChangeOrderItemQuantityAsync(order.Id, itemId, 3, Pin);

        withPin.EnsureSuccessStatusCode();
        var result = await PosApiClient.ReadAsync<OrderMutationResponse>(withPin);
        result.Order.Total.Should().Be(750m);
        result.Kot!.Kind.Should().Be("Modification");
        result.Kot.Lines.Single().Note.Should().Be("Was 1", "the kitchen needs to know what changed");
    }

    [Fact]
    public async Task VoidingAnItemOnAnOpenOrder_RequiresThePinAndPrintsACancellationKot()
    {
        await SignInAsAdminAsync();
        await ConfigurePinAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var sandwich = await CreateMenuItemAsync("Sandwich", 150m);
        var order = await OpenOrderAsync(tableId, (friedRice, 1), (sandwich, 1));
        var sandwichLine = order.Items.Single(i => i.MenuItemName == "Sandwich");

        var withoutPin = await Client.VoidOrderItemAsync(order.Id, sandwichLine.Id);
        withoutPin.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var withPin = await Client.VoidOrderItemAsync(order.Id, sandwichLine.Id, Pin);

        withPin.EnsureSuccessStatusCode();
        var result = await PosApiClient.ReadAsync<OrderMutationResponse>(withPin);
        result.Order.Total.Should().Be(250m);
        result.Order.Items.Should().HaveCount(2, "a voided line stays on the bill as a record");
        result.Order.Items.Single(i => i.MenuItemName == "Sandwich").IsCancelled.Should().BeTrue();
        result.Kot!.Kind.Should().Be("Cancellation");
        result.Kot.Lines.Single().Note.Should().Be("CANCELLED");
    }

    [Fact]
    public async Task EditingADraft_NeedsNoPinBecauseNothingHasBeenSentToTheKitchen()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);

        var created = await Client.CreateOrderAsync(tableId);
        var order = await PosApiClient.ReadAsync<OrderResponse>(created);
        var added = await Client.AddOrderItemsAsync(order.Id, (friedRice, 1, null));
        var itemId = (await PosApiClient.ReadAsync<OrderMutationResponse>(added)).Order.Items.Single().Id;

        var changed = await Client.ChangeOrderItemQuantityAsync(order.Id, itemId, 5);
        changed.EnsureSuccessStatusCode();
        (await PosApiClient.ReadAsync<OrderMutationResponse>(changed)).Kot
            .Should().BeNull("a draft has never reached the kitchen");

        var voided = await Client.VoidOrderItemAsync(order.Id, itemId);
        voided.EnsureSuccessStatusCode();
        (await PosApiClient.ReadAsync<OrderMutationResponse>(voided)).Order.Items
            .Should().BeEmpty("a draft line is simply deleted");
    }

    [Fact]
    public async Task PayingABill_IssuesAReceiptAndReleasesTheTable()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var sandwich = await CreateMenuItemAsync("Sandwich", 150m);
        var order = await OpenOrderAsync(tableId, (friedRice, 1), (sandwich, 1));

        (await Client.StartCheckoutAsync(order.Id)).EnsureSuccessStatusCode();

        var paid = await Client.PayOrderAsync(order.Id, ("Cash", 400m, 500m));

        paid.EnsureSuccessStatusCode();
        var receipt = await PosApiClient.ReadAsync<ReceiptDocumentResponse>(paid);

        receipt.ReceiptNumber.Should().Be($"REC-001-{DateTime.Now.Year}");
        receipt.RestaurantName.Should().Be("Sri Lakshmi Family Restaurant");
        receipt.Total.Should().Be(400m);
        receipt.TaxAmount.Should().Be(0m, "no VAT or GST is applied (BR-POS-011)");
        receipt.ChangeGiven.Should().Be(100m);
        receipt.Lines.Should().HaveCount(2);
        receipt.QrPayload.Should().NotBeNullOrWhiteSpace("the slip carries a QR code (POS-028)");

        var tables = await PosApiClient.ReadAsync<List<TableResponse>>(await Client.GetTablesAsync());
        tables.Single(t => t.Id == tableId).CurrentOrder
            .Should().BeNull("the table is free again once the bill is paid (POS-032)");
    }

    [Fact]
    public async Task PaymentsMustAddUpToTheBillExactly()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var order = await OpenOrderAsync(tableId, (friedRice, 1));
        await Client.StartCheckoutAsync(order.Id);

        var short_ = await Client.PayOrderAsync(order.Id, ("Cash", 200m, null));

        short_.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(short_)).Should().Be("Order.PaymentMismatch");
    }

    [Fact]
    public async Task ABillCanBeSplitAcrossSeveralPaymentMethods()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var sandwich = await CreateMenuItemAsync("Sandwich", 150m);
        var order = await OpenOrderAsync(tableId, (friedRice, 1), (sandwich, 1));
        await Client.StartCheckoutAsync(order.Id);

        var paid = await Client.PayOrderAsync(order.Id, ("Cash", 100m, null), ("Card", 300m, null));

        paid.EnsureSuccessStatusCode();
        var receipt = await PosApiClient.ReadAsync<ReceiptDocumentResponse>(paid);
        receipt.Payments.Should().HaveCount(2);
        receipt.Payments.Sum(p => p.Amount).Should().Be(400m);
    }

    [Fact]
    public async Task ADiscountComesOffTheBillAndCannotExceedIt()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var order = await OpenOrderAsync(tableId, (friedRice, 4));

        var tooBig = await Client.SetOrderDiscountAsync(order.Id, "Fixed", 5000m);
        tooBig.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(tooBig)).Should().Be("Order.DiscountExceedsSubtotal");

        var applied = await Client.SetOrderDiscountAsync(order.Id, "Percentage", 10m);

        applied.EnsureSuccessStatusCode();
        var result = await PosApiClient.ReadAsync<OrderMutationResponse>(applied);
        result.Order.Subtotal.Should().Be(1000m);
        result.Order.DiscountAmount.Should().Be(100m);
        result.Order.Total.Should().Be(900m);
    }

    [Fact]
    public async Task ReprintingAReceipt_ReproducesItAndCountsThePrint()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var order = await OpenOrderAsync(tableId, (friedRice, 1));
        await Client.StartCheckoutAsync(order.Id);
        var original = await PosApiClient.ReadAsync<ReceiptDocumentResponse>(
            await Client.PayOrderAsync(order.Id, ("Cash", 250m, null)));

        var reprint = await Client.ReprintReceiptAsync(order.Id);

        reprint.EnsureSuccessStatusCode();
        var copy = await PosApiClient.ReadAsync<ReceiptDocumentResponse>(reprint);
        copy.ReceiptNumber.Should().Be(original.ReceiptNumber);
        copy.Total.Should().Be(original.Total);
        copy.PrintCount.Should().Be(2, "the original print counts as the first");
    }

    [Fact]
    public async Task CancellingAConfirmedOrder_RequiresThePinAndFreesTheTable()
    {
        await SignInAsAdminAsync();
        await ConfigurePinAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var order = await OpenOrderAsync(tableId, (friedRice, 2));

        var withoutPin = await Client.CancelOrderAsync(order.Id);
        withoutPin.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var cancelled = await Client.CancelOrderAsync(order.Id, Pin, "Customer left");

        cancelled.EnsureSuccessStatusCode();
        var result = await PosApiClient.ReadAsync<OrderMutationResponse>(cancelled);
        result.Order.Status.Should().Be("Cancelled");
        result.Kot!.Kind.Should().Be("Cancellation");
        result.Kot.Lines.Single().Note.Should().Be("ORDER CANCELLED");

        var tables = await PosApiClient.ReadAsync<List<TableResponse>>(await Client.GetTablesAsync());
        tables.Single(t => t.Id == tableId).CurrentOrder.Should().BeNull();
    }

    [Fact]
    public async Task ThreeWrongPins_PauseFurtherAttemptsOnThatTerminal()
    {
        await SignInAsAdminAsync();
        await ConfigurePinAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var order = await OpenOrderAsync(tableId, (friedRice, 1));
        var itemId = order.Items.Single().Id;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var failed = await Client.ChangeOrderItemQuantityAsync(order.Id, itemId, 2, "0000");
            failed.IsSuccessStatusCode.Should().BeFalse();
        }

        // Even the correct PIN is refused during the cooldown.
        var lockedOut = await Client.ChangeOrderItemQuantityAsync(order.Id, itemId, 2, Pin);

        lockedOut.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await PosApiClient.ReadErrorCodeAsync(lockedOut)).Should().Be("Auth.PinAttemptsExhausted");
    }

    [Fact]
    public async Task TwoTablesAreServedAndSettledIndependently()
    {
        await SignInAsAdminAsync();
        var table2 = await CreateTableAsync("2");
        var table3 = await CreateTableAsync("3");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        var sandwich = await CreateMenuItemAsync("Sandwich", 150m);
        var kottu = await CreateMenuItemAsync("Kottu Roti", 300m);
        var lassi = await CreateMenuItemAsync("Mango Lassi", 50m);

        var order1 = await OpenOrderAsync(table2, (friedRice, 1));
        var order2 = await OpenOrderAsync(table3, (kottu, 1));

        order1.OrderNumber.Should().Be(1);
        order2.OrderNumber.Should().Be(2, "each order gets its own number (BR-POS-016)");

        await Client.AddOrderItemsAsync(order1.Id, (sandwich, 1, null));
        await Client.AddOrderItemsAsync(order2.Id, (lassi, 2, null));

        var openOrders = await PosApiClient.ReadAsync<List<OrderSummaryResponse>>(
            await Client.GetOrdersAsync("?openOnly=true"));
        openOrders.Should().HaveCount(2);
        openOrders.Single(o => o.OrderNumber == 1).Total.Should().Be(400m);
        openOrders.Single(o => o.OrderNumber == 2).Total.Should().Be(400m);

        // Settling table 2 must leave table 3 exactly as it was (BR-POS-019).
        await Client.StartCheckoutAsync(order1.Id);
        (await Client.PayOrderAsync(order1.Id, ("Cash", 400m, 400m))).EnsureSuccessStatusCode();

        var tables = await PosApiClient.ReadAsync<List<TableResponse>>(await Client.GetTablesAsync());
        tables.Single(t => t.Id == table2).CurrentOrder.Should().BeNull();
        tables.Single(t => t.Id == table3).CurrentOrder!.Total.Should().Be(400m);

        await Client.StartCheckoutAsync(order2.Id);
        (await Client.PayOrderAsync(order2.Id, ("Card", 400m, null))).EnsureSuccessStatusCode();

        var settled = await PosApiClient.ReadAsync<List<TableResponse>>(await Client.GetTablesAsync());
        settled.Should().OnlyContain(t => t.CurrentOrder == null, "every table is free again");
    }

    [Fact]
    public async Task OrdersCanBeSearchedByTableNumber()
    {
        await SignInAsAdminAsync();
        var table2 = await CreateTableAsync("2");
        var table7 = await CreateTableAsync("7");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        await OpenOrderAsync(table2, (friedRice, 1));
        await OpenOrderAsync(table7, (friedRice, 1));

        var found = await PosApiClient.ReadAsync<List<OrderSummaryResponse>>(
            await Client.GetOrdersAsync("?search=7"));

        found.Should().ContainSingle().Which.TableNumber.Should().Be("7");
    }

    [Fact]
    public async Task ATableWithALiveOrderCannotBeTakenOutOfService()
    {
        await SignInAsAdminAsync();
        var tableId = await CreateTableAsync("2");
        var friedRice = await CreateMenuItemAsync("Fried Rice", 250m);
        await OpenOrderAsync(tableId, (friedRice, 1));

        var response = await Client.SetTableActiveAsync(tableId, false);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Table.InUse");
    }

    [Fact]
    public async Task TableNumbersMustBeUnique()
    {
        await SignInAsAdminAsync();
        await CreateTableAsync("2");

        var duplicate = await Client.CreateTableAsync("2");

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(duplicate)).Should().Be("Table.NumberTaken");
    }

    [Fact]
    public async Task StaffWithoutPosBilling_CannotReachTheTill()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "KitchenOperations");

        (await staff.GetTablesAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.GetOrdersAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
