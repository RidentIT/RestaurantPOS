using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Shifts;

/// <summary>
/// A cashier checks in to start a shift and out to end it; everything they ring up in between
/// is totalled against that shift, and the next check-in starts again from zero.
/// </summary>
public class ShiftTests : IntegrationTestBase
{
    public ShiftTests() => AutoCheckIn = false;

    private async Task<Guid> CreateTableAsync(string number) =>
        (await PosApiClient.ReadAsync<TableResponse>(await Client.CreateTableAsync(number))).Id;

    private async Task<Guid> CreateDishAsync(string name, decimal price) =>
        (await PosApiClient.ReadAsync<MenuItemResponse>(await Client.CreateMenuItemAsync(name, "Mains", price)))
        .Variants.Single().Id;

    /// <summary>Rings a table's bill through to a settled cash payment on the given client's shift.</summary>
    private static async Task SellForCashAsync(PosApiClient till, Guid tableId, Guid dish, int quantity, decimal unitPrice)
    {
        var order = await PosApiClient.ReadAsync<OrderResponse>(await till.CreateOrderAsync(tableId));
        await till.AddOrderItemsAsync(order.Id, (dish, quantity, null));
        (await till.ConfirmOrderAsync(order.Id)).EnsureSuccessStatusCode();
        (await till.StartCheckoutAsync(order.Id)).EnsureSuccessStatusCode();
        var total = unitPrice * quantity;
        (await till.PayOrderAsync(order.Id, ("Cash", total, total))).EnsureSuccessStatusCode();
    }

    private async Task<(PosApiClient Cashier, Guid Id)> NewCashierAsync(string username)
    {
        var (cashier, id) = await CreateAndSignInStaffAsync(username, modules: "PosBilling");
        return (cashier, id);
    }

    [Fact]
    public async Task AnOrderCannotBeTakenBeforeCheckingIn()
    {
        await SignInAsAdminAsync();
        var table = await CreateTableAsync("1");

        var response = await Client.CreateOrderAsync(table);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Shift.NotCheckedIn");
    }

    [Fact]
    public async Task CheckingInTwice_IsRefused()
    {
        await SignInAsAdminAsync();

        (await Client.CheckInAsync(500m)).EnsureSuccessStatusCode();
        var again = await Client.CheckInAsync(500m);

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(again)).Should().Be("Shift.AlreadyCheckedIn");
    }

    [Fact]
    public async Task CurrentShift_IsNoneUntilCheckedIn()
    {
        await SignInAsAdminAsync();

        var before = await PosApiClient.ReadAsync<CurrentShiftResponse>(await Client.GetCurrentShiftAsync());
        before.Shift.Should().BeNull();

        await Client.CheckInAsync(250m);

        var after = await PosApiClient.ReadAsync<CurrentShiftResponse>(await Client.GetCurrentShiftAsync());
        after.Shift.Should().NotBeNull();
        after.Shift!.IsOpen.Should().BeTrue();
        after.Shift.OpeningCash.Should().Be(250m);
        after.Shift.Summary.OrdersCompleted.Should().Be(0);
    }

    [Fact]
    public async Task ACompleteShift_TotalsItsSales_AndTheNextCheckInStartsFromZero()
    {
        await SignInAsAdminAsync();
        var dish = await CreateDishAsync("Kottu", 500m);
        var (cashier, _) = await NewCashierAsync("cashier-a");

        (await cashier.CheckInAsync(1000m)).EnsureSuccessStatusCode();
        await SellForCashAsync(cashier, await CreateTableAsync("1"), dish, quantity: 3, unitPrice: 500m); // 1500 cash

        var running = (await PosApiClient.ReadAsync<CurrentShiftResponse>(await cashier.GetCurrentShiftAsync())).Shift!;
        running.Summary.OrdersCompleted.Should().Be(1);
        running.Summary.NetSales.Should().Be(1500m);
        running.ExpectedCash.Should().Be(2500m, "the 1000 float plus 1500 of cash sales");

        var closed = await PosApiClient.ReadAsync<ShiftResponse>(await cashier.CheckOutAsync(2500m, "All good"));
        closed.IsOpen.Should().BeFalse();
        closed.ClosingCash.Should().Be(2500m);
        closed.Variance.Should().Be(0m);
        closed.Note.Should().Be("All good");

        (await cashier.CheckInAsync(800m)).EnsureSuccessStatusCode();
        var fresh = (await PosApiClient.ReadAsync<CurrentShiftResponse>(await cashier.GetCurrentShiftAsync())).Shift!;
        fresh.Id.Should().NotBe(closed.Id);
        fresh.Summary.OrdersCompleted.Should().Be(0, "a new shift starts from the beginning");
        fresh.Summary.NetSales.Should().Be(0m);
        fresh.ExpectedCash.Should().Be(800m);
    }

    [Fact]
    public async Task CountingLessCashThanExpected_ShowsAsAShortage()
    {
        await SignInAsAdminAsync();
        var dish = await CreateDishAsync("Rice", 400m);
        var (cashier, _) = await NewCashierAsync("cashier-a");

        await cashier.CheckInAsync(0m);
        await SellForCashAsync(cashier, await CreateTableAsync("1"), dish, 1, 400m);

        var closed = await PosApiClient.ReadAsync<ShiftResponse>(await cashier.CheckOutAsync(350m));

        closed.ExpectedCash.Should().Be(400m);
        closed.Variance.Should().Be(-50m);
    }

    [Fact]
    public async Task CheckingOut_IsRefusedWhileAnOrderIsStillUnsettled()
    {
        await SignInAsAdminAsync();
        var (cashier, _) = await NewCashierAsync("cashier-a");
        await cashier.CheckInAsync(0m);
        await cashier.CreateOrderAsync(await CreateTableAsync("1"));

        var response = await cashier.CheckOutAsync(0m);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Shift.HasOpenOrders");
    }

    [Fact]
    public async Task CheckingOutWithoutAShift_IsRefused()
    {
        await SignInAsAdminAsync();
        var (cashier, _) = await NewCashierAsync("cashier-a");

        var response = await cashier.CheckOutAsync(0m);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Shift.NoOpenShift");
    }

    [Fact]
    public async Task ACashier_SeesOnlyTheirOwnShifts_ButTheOwnerSeesEveryones()
    {
        await SignInAsAdminAsync();
        var (first, firstId) = await NewCashierAsync("cashier-a");
        var (second, _) = await NewCashierAsync("cashier-b");

        await first.CheckInAsync(100m);
        await second.CheckInAsync(200m);

        var mine = await PosApiClient.ReadAsync<List<ShiftResponse>>(await first.GetMyShiftsAsync());
        mine.Should().ContainSingle().Which.CashierUserId.Should().Be(firstId);

        (await first.GetShiftsAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var all = await PosApiClient.ReadAsync<List<ShiftResponse>>(await Client.GetShiftsAsync());
        all.Should().HaveCount(2);

        var onlyFirst = await PosApiClient.ReadAsync<List<ShiftResponse>>(await Client.GetShiftsAsync(firstId));
        onlyFirst.Should().ContainSingle();
    }

    [Fact]
    public async Task ACashier_CanOpenTheirOwnShiftDetail_ButNotSomeoneElses()
    {
        await SignInAsAdminAsync();
        var dish = await CreateDishAsync("Kottu", 500m);
        var (first, _) = await NewCashierAsync("cashier-a");
        var (second, _) = await NewCashierAsync("cashier-b");

        var firstShift = await PosApiClient.ReadAsync<ShiftResponse>(await first.CheckInAsync(0m));
        await second.CheckInAsync(0m);
        await SellForCashAsync(first, await CreateTableAsync("1"), dish, 2, 500m);

        var detail = await PosApiClient.ReadAsync<ShiftDetailResponse>(await first.GetShiftAsync(firstShift.Id));
        detail.Orders.Should().ContainSingle().Which.Total.Should().Be(1000m);

        (await second.GetShiftAsync(firstShift.Id)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.GetShiftAsync(firstShift.Id)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TheOwner_CanCloseAShiftTheCashierForgotAbout_EvenWithAnUnpaidOrder()
    {
        await SignInAsAdminAsync();
        var (cashier, _) = await NewCashierAsync("cashier-a");
        var shift = await PosApiClient.ReadAsync<ShiftResponse>(await cashier.CheckInAsync(300m));
        await cashier.CreateOrderAsync(await CreateTableAsync("1"));

        (await cashier.ForceCloseShiftAsync(shift.Id)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var closed = await PosApiClient.ReadAsync<ShiftResponse>(await Client.ForceCloseShiftAsync(shift.Id, note: "Left early"));
        closed.IsOpen.Should().BeFalse();
        closed.ClosingCash.Should().BeNull("nobody counted the drawer");
        closed.Note.Should().Be("Left early");

        var again = await Client.ForceCloseShiftAsync(shift.Id);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SomeoneWithoutPosBilling_CannotCheckIn()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync("reports-only", modules: "ReportsAnalytics");

        (await staff.CheckInAsync(0m)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
