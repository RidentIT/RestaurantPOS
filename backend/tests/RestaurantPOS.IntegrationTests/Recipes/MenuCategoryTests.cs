using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Recipes;

/// <summary>
/// The category picker behind the "Add menu item" dialog: a registered list, plus whatever a
/// menu item is already using even if nobody registered it.
/// </summary>
public class MenuCategoryTests : IntegrationTestBase
{
    [Fact]
    public async Task ARegisteredCategory_IsOfferedEvenBeforeAnyMenuItemUsesIt()
    {
        await SignInAsAdminAsync();

        var created = await Client.CreateMenuCategoryAsync("Grills");
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        (await PosApiClient.ReadAsync<string>(created)).Should().Be("Grills");

        var categories = await PosApiClient.ReadAsync<List<string>>(await Client.GetMenuCategoriesAsync());
        categories.Should().Contain("Grills");
    }

    [Fact]
    public async Task RegisteringTheSameCategoryTwice_IsRejectedRegardlessOfCasing()
    {
        await SignInAsAdminAsync();
        await Client.CreateMenuCategoryAsync("Beverages");

        var duplicate = await Client.CreateMenuCategoryAsync("BEVERAGES");

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(duplicate)).Should().Be("MenuCategory.NameTaken");
    }

    [Fact]
    public async Task RegisteringACategoryAlreadyUsedByAMenuItem_IsAlsoRejected()
    {
        await SignInAsAdminAsync();
        await Client.CreateMenuItemAsync("Chicken Fried Rice", "Rice & Curry", 850m);

        var duplicate = await Client.CreateMenuCategoryAsync("rice & curry");

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ACategoryUsedByAMenuItem_IsOfferedEvenIfNeverExplicitlyRegistered()
    {
        await SignInAsAdminAsync();
        await Client.CreateMenuItemAsync("Chicken Fried Rice", "Rice & Curry", 850m);

        var categories = await PosApiClient.ReadAsync<List<string>>(await Client.GetMenuCategoriesAsync());

        categories.Should().Contain("Rice & Curry");
    }

    [Fact]
    public async Task ACategoryAppearsOnlyOnce_EvenWhenBothRegisteredAndInUse()
    {
        await SignInAsAdminAsync();
        await Client.CreateMenuItemAsync("Chicken Fried Rice", "Rice & Curry", 850m);
        await Client.CreateMenuCategoryAsync("Rice & Curry");

        var categories = await PosApiClient.ReadAsync<List<string>>(await Client.GetMenuCategoriesAsync());

        categories.Where(c => c.Equals("Rice & Curry", StringComparison.OrdinalIgnoreCase))
            .Should().ContainSingle();
    }

    [Fact]
    public async Task StaffWithoutRecipeManagement_CannotReadOrRegisterCategories()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        (await staff.GetMenuCategoriesAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.CreateMenuCategoryAsync("Desserts")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ARegisteredCategoryWithNoMenuItems_CanBeDeleted()
    {
        await SignInAsAdminAsync();
        await Client.CreateMenuCategoryAsync("Grills");

        var deleted = await Client.DeleteMenuCategoryAsync("Grills");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var categories = await PosApiClient.ReadAsync<List<string>>(await Client.GetMenuCategoriesAsync());
        categories.Should().NotContain("Grills");
    }

    [Fact]
    public async Task ACategoryStillUsedByAMenuItem_CannotBeDeleted()
    {
        await SignInAsAdminAsync();
        await Client.CreateMenuItemAsync("Chicken Fried Rice", "Rice & Curry", 850m);
        await Client.CreateMenuItemAsync("Mutton Curry", "Rice & Curry", 950m);

        var deleted = await Client.DeleteMenuCategoryAsync("Rice & Curry");

        deleted.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(deleted)).Should().Be("MenuCategory.InUse");

        var categories = await PosApiClient.ReadAsync<List<string>>(await Client.GetMenuCategoriesAsync());
        categories.Should().Contain("Rice & Curry", "the category must still be offered while items use it");
    }

    [Fact]
    public async Task DeletingAnUnknownCategory_IsReportedAsNotFound()
    {
        await SignInAsAdminAsync();

        var deleted = await Client.DeleteMenuCategoryAsync("Never Existed");

        deleted.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StaffWithoutRecipeManagement_CannotDeleteCategories()
    {
        await SignInAsAdminAsync();
        await Client.CreateMenuCategoryAsync("Grills");
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        (await staff.DeleteMenuCategoryAsync("Grills")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
