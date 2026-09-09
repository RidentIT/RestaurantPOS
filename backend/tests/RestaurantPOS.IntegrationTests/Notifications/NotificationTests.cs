using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Notifications;

/// <summary>
/// Covers the three gates every notification passes: is this person eligible, have they switched
/// it off, and has it already been said.
/// </summary>
public class NotificationTests : IntegrationTestBase
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private async Task<NotificationFeedResponse> FeedAsync(PosApiClient? client = null)
    {
        var response = await (client ?? Client).GetNotificationsAsync();
        response.EnsureSuccessStatusCode();

        return await PosApiClient.ReadAsync<NotificationFeedResponse>(response);
    }

    /// <summary>Creates a raw material that is already below its reorder level.</summary>
    private async Task SeedLowStockAsync(string name = "Rice")
    {
        var material = await PosApiClient.ReadAsync<RawMaterialResponse>(
            await Client.CreateRawMaterialAsync(name, "Kilogram", mainStoreReorderLevel: 10m));

        // Named after the material so calling this twice does not collide on the supplier name.
        var supplier = await PosApiClient.ReadAsync<SupplierResponse>(
            await Client.CreateSupplierAsync($"{name} Wholesale"));

        // Two received, against a reorder level of ten.
        (await Client.CreateGoodsReceivedNoteAsync(supplier.Id, [(material.Id, 2m)])).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task EvaluatingRaisesALowStockAlert()
    {
        await SignInAsAdminAsync();
        await SeedLowStockAsync();

        var evaluated = await Client.EvaluateNotificationsAsync();
        evaluated.EnsureSuccessStatusCode();
        (await PosApiClient.ReadAsync<int>(evaluated)).Should().BeGreaterThan(0);

        var feed = await FeedAsync();

        feed.UnreadCount.Should().BeGreaterThan(0);
        feed.Notifications.Should().Contain(n => n.Type == "MainStoreLowStock");

        var alert = feed.Notifications.First(n => n.Type == "MainStoreLowStock");
        alert.Title.Should().Contain("Rice");
        alert.Body.Should().Contain("reorder level is 10");
        alert.Link.Should().Be("/inventory/main-store", "an alert should be something you can act on");
        alert.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task TheSameConditionIsNotRaisedTwice()
    {
        await SignInAsAdminAsync();
        await SeedLowStockAsync();

        await Client.EvaluateNotificationsAsync();
        var afterFirst = await FeedAsync();

        // Exactly what the bell does every minute.
        await Client.EvaluateNotificationsAsync();
        await Client.EvaluateNotificationsAsync();
        var afterThird = await FeedAsync();

        afterThird.Notifications.Count(n => n.Type == "MainStoreLowStock")
            .Should().Be(afterFirst.Notifications.Count(n => n.Type == "MainStoreLowStock"),
                "re-announcing a standing condition every minute would make the bell worthless");
    }

    [Fact]
    public async Task NegativeKitchenStockIsRaisedAsUrgent()
    {
        await SignInAsAdminAsync();

        var rice = await PosApiClient.ReadAsync<RawMaterialResponse>(
            await Client.CreateRawMaterialAsync("Rice", "Kilogram"));
        var item = await PosApiClient.ReadAsync<MenuItemResponse>(
            await Client.CreateMenuItemAsync("Fried Rice", "Mains", 250m));
        var variantId = item.Variants.Single().Id;

        await Client.UpsertRecipeAsync(variantId, (rice.Id, 1m));

        // Nothing was ever released to the kitchen, so this drives the balance negative.
        (await Client.ConsumeStockAsync(variantId, 2)).EnsureSuccessStatusCode();

        await Client.EvaluateNotificationsAsync();
        var feed = await FeedAsync();

        var alert = feed.Notifications.Single(n => n.Type == "KitchenStockNegative");
        alert.Severity.Should().Be("Urgent");
        alert.Body.Should().Contain("never booked in");
    }

    [Fact]
    public async Task AUserOnlyReceivesNotificationsForModulesTheyHold()
    {
        await SignInAsAdminAsync();
        await SeedLowStockAsync();

        // A cashier holds POS only, so a stock alert is none of their business.
        var (cashier, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        await Client.EvaluateNotificationsAsync();

        var adminFeed = await FeedAsync();
        var cashierFeed = await FeedAsync(cashier);

        adminFeed.Notifications.Should().Contain(n => n.Type == "MainStoreLowStock");
        cashierFeed.Notifications.Should().NotContain(n => n.Type == "MainStoreLowStock");
    }

    [Fact]
    public async Task SwitchingANotificationOffStopsItBeingRaised()
    {
        await SignInAsAdminAsync();
        await SeedLowStockAsync();

        (await Client.UpdateNotificationPreferenceAsync("MainStoreLowStock", isEnabled: false))
            .EnsureSuccessStatusCode();

        await Client.EvaluateNotificationsAsync();
        var feed = await FeedAsync();

        feed.Notifications.Should().NotContain(n => n.Type == "MainStoreLowStock");
    }

    [Fact]
    public async Task ANotificationOffByDefaultStaysQuietUntilItIsTurnedOn()
    {
        await SignInAsAdminAsync();

        // A menu item with no recipe — off by default in the catalog.
        await Client.CreateMenuItemAsync("Bottled Water", "Beverages", 80m);

        await Client.EvaluateNotificationsAsync();
        (await FeedAsync()).Notifications.Should().NotContain(n => n.Type == "MenuItemWithoutRecipe");

        (await Client.UpdateNotificationPreferenceAsync("MenuItemWithoutRecipe", isEnabled: true))
            .EnsureSuccessStatusCode();

        await Client.EvaluateNotificationsAsync();
        (await FeedAsync()).Notifications.Should().Contain(n => n.Type == "MenuItemWithoutRecipe");
    }

    [Fact]
    public async Task MarkingReadClearsTheUnreadCount()
    {
        await SignInAsAdminAsync();
        await SeedLowStockAsync();
        await Client.EvaluateNotificationsAsync();

        var before = await FeedAsync();
        before.UnreadCount.Should().BeGreaterThan(0);

        (await Client.MarkNotificationsReadAsync()).EnsureSuccessStatusCode();

        var after = await FeedAsync();
        after.UnreadCount.Should().Be(0);
        after.Notifications.Should().OnlyContain(n => n.IsRead, "they are read, not deleted");
    }

    [Fact]
    public async Task MarkingOneReadLeavesTheRest()
    {
        await SignInAsAdminAsync();
        await SeedLowStockAsync("Rice");
        await SeedLowStockAsync("Flour");
        await Client.EvaluateNotificationsAsync();

        var feed = await FeedAsync();
        feed.UnreadCount.Should().BeGreaterThan(1);

        (await Client.MarkNotificationsReadAsync(feed.Notifications.First().Id)).EnsureSuccessStatusCode();

        (await FeedAsync()).UnreadCount.Should().Be(feed.UnreadCount - 1);
    }

    [Fact]
    public async Task OneUserReadingSomethingDoesNotClearItForAnother()
    {
        await SignInAsAdminAsync();
        await SeedLowStockAsync();
        var (manager, _) = await CreateAndSignInStaffAsync(modules: "StoreStockManagement");

        await Client.EvaluateNotificationsAsync();

        (await Client.MarkNotificationsReadAsync()).EnsureSuccessStatusCode();

        (await FeedAsync()).UnreadCount.Should().Be(0);
        (await FeedAsync(manager)).UnreadCount
            .Should().BeGreaterThan(0, "dismissing your own bell must not hide it from anyone else");
    }

    [Fact]
    public async Task PreferencesOnlyListWhatTheUserCouldEverReceive()
    {
        await SignInAsAdminAsync();
        var (cashier, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        var response = await cashier.GetNotificationPreferencesAsync();
        response.EnsureSuccessStatusCode();
        var preferences = await PosApiClient.ReadAsync<List<NotificationPreferenceResponse>>(response);

        preferences.Should().NotBeEmpty();
        preferences.Should().Contain(p => p.Type == "FoodReadyToServe");
        preferences.Should().NotContain(p => p.Type == "SupplierPaymentDue",
            "an option you can never receive is only clutter");
        preferences.Should().OnlyContain(p => p.IsDefault, "nothing has been changed yet");
    }

    [Fact]
    public async Task AnAdministratorSeesEveryNotificationInThePreferences()
    {
        await SignInAsAdminAsync();

        var preferences = await PosApiClient.ReadAsync<List<NotificationPreferenceResponse>>(
            await Client.GetNotificationPreferencesAsync());

        preferences.Should().Contain(p => p.Type == "SupplierPaymentDue");
        preferences.Should().Contain(p => p.Type == "FoodReadyToServe");
        preferences.Should().Contain(p => p.HasThreshold && p.Threshold != null);
    }

    [Fact]
    public async Task AThresholdChangesWhenTheRuleFires()
    {
        await SignInAsAdminAsync();

        var thresholded = await PosApiClient.ReadAsync<List<NotificationPreferenceResponse>>(
            await Client.GetNotificationPreferencesAsync());

        thresholded.Single(p => p.Type == "KitchenTicketWaitingTooLong").Threshold.Should().Be(15m);

        (await Client.UpdateNotificationThresholdAsync("KitchenTicketWaitingTooLong", 25m))
            .EnsureSuccessStatusCode();

        var updated = await PosApiClient.ReadAsync<List<NotificationPreferenceResponse>>(
            await Client.GetNotificationPreferencesAsync());

        updated.Single(p => p.Type == "KitchenTicketWaitingTooLong").Threshold.Should().Be(25m);
    }

    [Fact]
    public async Task ANotificationWithoutAThresholdCannotBeGivenOne()
    {
        await SignInAsAdminAsync();

        var response = await Client.UpdateNotificationThresholdAsync("KitchenStockNegative", 5m);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Notification.NoThreshold");
    }

    [Fact]
    public async Task OnlyAnAdministratorCanChangeAThreshold()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "KitchenOperations");

        var response = await staff.UpdateNotificationThresholdAsync("KitchenTicketWaitingTooLong", 30m);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "how long a dish may sit on the pass is a fact about the kitchen, not a personal setting");
    }

    [Fact]
    public async Task EveryoneCanReachTheirOwnBellWhateverModulesTheyHold()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        // Deliberately not gated on the Notifications module: a cashier without it still needs
        // to be told their food is ready.
        (await staff.GetNotificationsAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
        (await staff.EvaluateNotificationsAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnOrderCancellationReachesBothTheTillAndTheKitchen()
    {
        await SignInAsAdminAsync();
        (await Client.SetApprovalPinAsync(AdminPassword, "4417")).EnsureSuccessStatusCode();

        var table = await PosApiClient.ReadAsync<TableResponse>(await Client.CreateTableAsync("2"));
        var item = await PosApiClient.ReadAsync<MenuItemResponse>(
            await Client.CreateMenuItemAsync("Fried Rice", "Mains", 250m));

        var order = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(table.Id));
        await Client.AddOrderItemsAsync(order.Id, (item.Variants.Single().Id, 1, null));
        (await Client.ConfirmOrderAsync(order.Id)).EnsureSuccessStatusCode();
        (await Client.CancelOrderAsync(order.Id, "4417", "Customer left")).EnsureSuccessStatusCode();

        var (cashier, _) = await CreateAndSignInStaffAsync("cashier01", modules: "PosBilling");
        var (chef, _) = await CreateAndSignInStaffAsync("chef01", "Chef@2026x", "KitchenOperations");

        await Client.EvaluateNotificationsAsync();

        (await FeedAsync(cashier)).Notifications.Should().Contain(n => n.Type == "OrderCancelled");
        (await FeedAsync(chef)).Notifications.Should().Contain(n => n.Type == "OrderCancelled");
    }
}
