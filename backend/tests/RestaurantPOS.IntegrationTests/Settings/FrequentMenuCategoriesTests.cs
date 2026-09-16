using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Settings;

/// <summary>
/// The categories pinned to the front of a till's category strip — a cashier's own shortcut, not
/// an administrator's setting, so it lives behind POS &amp; Billing rather than System Settings.
/// </summary>
public class FrequentMenuCategoriesTests : IntegrationTestBase
{
    [Fact]
    public async Task NewRestaurant_StartsWithTheSeededDefaults()
    {
        await SignInAsAdminAsync();

        var categories = await PosApiClient.ReadAsync<List<string>>(await Client.GetFrequentMenuCategoriesAsync());

        categories.Should().Equal("Kottu", "Rice and Curry", "Cheese Kottu", "String Hoppers");
    }

    [Fact]
    public async Task PosBillingHolders_CanPinAndUnpinCategories_WithoutSystemSettings()
    {
        await SignInAsAdminAsync();
        var (cashier, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        var afterAdd = await PosApiClient.ReadAsync<List<string>>(
            await cashier.AddFrequentMenuCategoryAsync("Desserts"));
        afterAdd.Should().ContainInOrder("Kottu", "Rice and Curry", "Cheese Kottu", "String Hoppers", "Desserts");

        var afterRemove = await PosApiClient.ReadAsync<List<string>>(
            await cashier.RemoveFrequentMenuCategoryAsync("Rice and Curry"));
        afterRemove.Should().Equal("Kottu", "Cheese Kottu", "String Hoppers", "Desserts");
    }

    [Fact]
    public async Task AddingTheSameCategoryTwice_IsANoOp()
    {
        await SignInAsAdminAsync();

        (await Client.AddFrequentMenuCategoryAsync("kottu")).EnsureSuccessStatusCode(); // different casing

        var categories = await PosApiClient.ReadAsync<List<string>>(await Client.GetFrequentMenuCategoriesAsync());
        categories.Should().Equal("Kottu", "Rice and Curry", "Cheese Kottu", "String Hoppers");
    }

    [Fact]
    public async Task RemovingACategoryThatIsNotPinned_IsANoOp()
    {
        await SignInAsAdminAsync();

        var categories = await PosApiClient.ReadAsync<List<string>>(
            await Client.RemoveFrequentMenuCategoryAsync("Beverages"));

        categories.Should().Equal("Kottu", "Rice and Curry", "Cheese Kottu", "String Hoppers");
    }

    [Fact]
    public async Task SomeoneWithoutPosBilling_CannotReadOrChangeFrequentCategories()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "ReportsAnalytics");

        (await staff.GetFrequentMenuCategoriesAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.AddFrequentMenuCategoryAsync("Desserts")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
