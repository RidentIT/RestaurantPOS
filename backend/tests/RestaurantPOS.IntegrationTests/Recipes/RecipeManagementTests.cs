using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Recipes;

public class RecipeManagementTests : IntegrationTestBase
{
    [Fact]
    public async Task CreatingAMenuItem_StartsWithNoRecipe()
    {
        await SignInAsAdminAsync();

        var response = await Client.CreateMenuItemAsync("Chicken Fried Rice", "Rice & Curry", 850m);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await PosApiClient.ReadAsync<MenuItemResponse>(response);
        item.HasRecipe.Should().BeFalse();
        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task MenuItemNamesAreUniqueRegardlessOfCasing()
    {
        await SignInAsAdminAsync();
        await Client.CreateMenuItemAsync("Chicken Fried Rice", "Rice & Curry", 850m);

        var duplicate = await Client.CreateMenuItemAsync("CHICKEN FRIED RICE", "Rice & Curry", 900m);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(duplicate)).Should().Be("MenuItem.NameTaken");
    }

    [Fact]
    public async Task ANegativePriceIsRejected()
    {
        await SignInAsAdminAsync();

        var response = await Client.CreateMenuItemAsync("Fried Rice", "Rice", -1m);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatingARecipe_RequiresAtLeastOneLine()
    {
        await SignInAsAdminAsync();
        var itemId = await CreateMenuItemAsync();

        var response = await Client.UpsertRecipeAsync(itemId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatingARecipe_RejectsAZeroQuantity()
    {
        await SignInAsAdminAsync();
        var itemId = await CreateMenuItemAsync();
        var riceId = await CreateRawMaterialAsync("Rice");

        var response = await Client.UpsertRecipeAsync(itemId, (riceId, 0m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatingARecipe_RejectsTheSameRawMaterialTwice()
    {
        await SignInAsAdminAsync();
        var itemId = await CreateMenuItemAsync();
        var riceId = await CreateRawMaterialAsync("Rice");

        var response = await Client.UpsertRecipeAsync(itemId, (riceId, 1m), (riceId, 2m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatingARecipe_RejectsAnUnknownRawMaterial()
    {
        await SignInAsAdminAsync();
        var itemId = await CreateMenuItemAsync();

        var response = await Client.UpsertRecipeAsync(itemId, (Guid.NewGuid(), 1m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Recipe.UnknownRawMaterial");
    }

    [Fact]
    public async Task CreatingARecipe_RejectsAnInactiveRawMaterial()
    {
        await SignInAsAdminAsync();
        var itemId = await CreateMenuItemAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        await Client.SetRawMaterialActiveAsync(riceId, false);

        var response = await Client.UpsertRecipeAsync(itemId, (riceId, 1m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Recipe.InactiveRawMaterial");
    }

    [Fact]
    public async Task UpsertingASecondTime_ReplacesTheLinesRatherThanMergingThem()
    {
        await SignInAsAdminAsync();
        var itemId = await CreateMenuItemAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var chickenId = await CreateRawMaterialAsync("Chicken");
        var oilId = await CreateRawMaterialAsync("Cooking Oil", "Liter");

        await Client.UpsertRecipeAsync(itemId, (riceId, 0.25m), (chickenId, 0.15m));
        var replaced = await Client.UpsertRecipeAsync(itemId, (riceId, 0.3m), (oilId, 0.05m));

        var recipe = await PosApiClient.ReadAsync<RecipeResponse>(replaced);
        recipe.Lines.Should().HaveCount(2);
        recipe.Lines.Should().Contain(l => l.RawMaterialId == riceId && l.Quantity == 0.3m);
        recipe.Lines.Should().Contain(l => l.RawMaterialId == oilId);
        recipe.Lines.Should().NotContain(l => l.RawMaterialId == chickenId);

        // The replacement must be durable, not just reflected in the handler's in-memory response.
        var reFetched = await PosApiClient.ReadAsync<RecipeResponse>(await Client.GetRecipeAsync(itemId));
        reFetched.Lines.Should().HaveCount(2);
        reFetched.Lines.Should().NotContain(l => l.RawMaterialId == chickenId);
    }

    [Fact]
    public async Task ANewMenuItem_ReportsHasRecipeOnceOneExists()
    {
        await SignInAsAdminAsync();
        var itemId = await CreateMenuItemAsync();
        var riceId = await CreateRawMaterialAsync("Rice");

        await Client.UpsertRecipeAsync(itemId, (riceId, 0.25m));

        var item = await PosApiClient.ReadAsync<MenuItemResponse>(await Client.GetMenuItemAsync(itemId));
        item.HasRecipe.Should().BeTrue();
    }

    [Fact]
    public async Task DisablingARecipe_KeepsItsLinesVisible()
    {
        await SignInAsAdminAsync();
        var itemId = await CreateMenuItemAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        await Client.UpsertRecipeAsync(itemId, (riceId, 0.25m));

        var disabled = await PosApiClient.ReadAsync<RecipeResponse>(
            await Client.SetRecipeEnabledAsync(itemId, false));

        disabled.IsEnabled.Should().BeFalse();
        disabled.Lines.Should().ContainSingle();
    }

    [Fact]
    public async Task DeletingARecipe_AllowsANewOneToBeCreatedAfterwards()
    {
        await SignInAsAdminAsync();
        var itemId = await CreateMenuItemAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        await Client.UpsertRecipeAsync(itemId, (riceId, 0.25m));

        (await Client.DeleteRecipeAsync(itemId)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await Client.GetRecipeAsync(itemId);
        afterDelete.StatusCode.Should().Be(HttpStatusCode.OK);
        (await afterDelete.Content.ReadAsStringAsync()).Should().BeEmpty("no recipe exists for this menu item any more");

        var recreated = await Client.UpsertRecipeAsync(itemId, (riceId, 0.5m));
        recreated.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StaffWithoutTheModuleCannotReachMenuItems()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "StoreStockManagement");

        (await staff.GetMenuItemsAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StaffGrantedRecipeManagement_CanManageMenuItems()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "RecipeManagement");

        (await staff.CreateMenuItemAsync("Fried Rice", "Rice", 700m))
            .StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<Guid> CreateMenuItemAsync(string name = "Chicken Fried Rice") =>
        (await PosApiClient.ReadAsync<MenuItemResponse>(await Client.CreateMenuItemAsync(name, "Rice & Curry", 850m))).Id;

    private async Task<Guid> CreateRawMaterialAsync(string name, string unit = "Kilogram") =>
        (await PosApiClient.ReadAsync<RawMaterialResponse>(await Client.CreateRawMaterialAsync(name, unit))).Id;
}