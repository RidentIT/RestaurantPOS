using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Inventory;

public class InventoryTests : IntegrationTestBase
{
    [Fact]
    public async Task ANewRawMaterial_StartsWithZeroStockInBothStores()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");

        var mainStore = await ReadStockAsync(await Client.GetMainStoreStockAsync());
        var kitchen = await ReadStockAsync(await Client.GetKitchenStockAsync());

        mainStore.Should().Contain(s => s.RawMaterialId == riceId && s.QuantityOnHand == 0);
        kitchen.Should().Contain(s => s.RawMaterialId == riceId && s.QuantityOnHand == 0);
    }

    [Fact]
    public async Task RawMaterialNamesAreUniqueRegardlessOfCasing()
    {
        await SignInAsAdminAsync();
        await Client.CreateRawMaterialAsync("Rice");

        var duplicate = await Client.CreateRawMaterialAsync("RICE");

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(duplicate)).Should().Be("RawMaterial.NameTaken");
    }

    [Fact]
    public async Task ReceivingGoods_IncreasesMainStoreStockImmediately()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var supplierId = await CreateSupplierAsync();

        var response = await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 20m)], "Weekly delivery");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var stock = await ReadStockAsync(await Client.GetMainStoreStockAsync());
        stock.Should().Contain(s => s.RawMaterialId == riceId && s.QuantityOnHand == 20m);
    }

    [Fact]
    public async Task ReceivingGoods_RejectsAnInactiveSupplier()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var supplierId = await CreateSupplierAsync();
        await Client.SetSupplierActiveAsync(supplierId, false);

        var response = await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 5m)]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Supplier.Inactive");
    }

    [Fact]
    public async Task LowStockIsFlaggedOnceTheReorderLevelIsReached()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice", mainStoreReorderLevel: 10m);
        var supplierId = await CreateSupplierAsync();

        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 10m)]);

        var stock = await ReadStockAsync(await Client.GetMainStoreStockAsync());
        stock.Should().Contain(s => s.RawMaterialId == riceId && s.IsLowStock);
    }

    [Fact]
    public async Task ReleasingStock_RequiresAValidApprovalPin()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var supplierId = await CreateSupplierAsync();
        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 20m)]);
        await SetApprovalPinAsync();

        var response = await Client.CreateStockReleaseAsync([(riceId, 5m)], "0000");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Auth.InvalidPin");
    }

    [Fact]
    public async Task ReleasingStock_WithNoAdminPinConfiguredAnywhere_FailsClearly()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var supplierId = await CreateSupplierAsync();
        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 20m)]);

        var response = await Client.CreateStockReleaseAsync([(riceId, 5m)], "0000");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Auth.NoAdminPinConfigured");
    }

    [Fact]
    public async Task ReleasingStock_MovesItFromMainStoreToKitchen()
    {
        var admin = await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var supplierId = await CreateSupplierAsync();
        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 20m)]);
        var pin = await SetApprovalPinAsync();

        var response = await Client.CreateStockReleaseAsync([(riceId, 5m)], pin, "Morning prep");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var release = await PosApiClient.ReadAsync<StockReleaseResponse>(response);
        release.ApprovedByName.Should().Be(admin.User.FullName);
        release.Lines.Should().ContainSingle(l => l.RawMaterialId == riceId && l.Quantity == 5m);

        var mainStore = await ReadStockAsync(await Client.GetMainStoreStockAsync());
        var kitchen = await ReadStockAsync(await Client.GetKitchenStockAsync());
        mainStore.Should().Contain(s => s.RawMaterialId == riceId && s.QuantityOnHand == 15m);
        kitchen.Should().Contain(s => s.RawMaterialId == riceId && s.QuantityOnHand == 5m);
    }

    [Fact]
    public async Task ReleasingMoreThanIsInMainStock_IsBlockedAndChangesNothing()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var supplierId = await CreateSupplierAsync();
        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 5m)]);
        var pin = await SetApprovalPinAsync();

        var response = await Client.CreateStockReleaseAsync([(riceId, 10m)], pin);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Inventory.InsufficientStock");

        var mainStore = await ReadStockAsync(await Client.GetMainStoreStockAsync());
        mainStore.Should().Contain(s => s.RawMaterialId == riceId && s.QuantityOnHand == 5m, "a rejected release must not partially apply");
    }

    [Fact]
    public async Task ReleasingTwoRawMaterialsWhereOnlyOneHasEnoughStock_AppliesNeitherOfThem()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var chickenId = await CreateRawMaterialAsync("Chicken");
        var supplierId = await CreateSupplierAsync();
        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 20m), (chickenId, 2m)]);
        var pin = await SetApprovalPinAsync();

        // Rice has plenty; chicken does not — the whole release must fail atomically.
        var response = await Client.CreateStockReleaseAsync([(riceId, 5m), (chickenId, 10m)], pin);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var mainStore = await ReadStockAsync(await Client.GetMainStoreStockAsync());
        mainStore.Should().Contain(s => s.RawMaterialId == riceId && s.QuantityOnHand == 20m);
        mainStore.Should().Contain(s => s.RawMaterialId == chickenId && s.QuantityOnHand == 2m);
    }

    [Fact]
    public async Task ConsumingStock_DeductsKitchenStockAccordingToTheRecipe()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var chickenId = await CreateRawMaterialAsync("Chicken");
        var supplierId = await CreateSupplierAsync();
        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 20m), (chickenId, 10m)]);
        var pin = await SetApprovalPinAsync();
        await Client.CreateStockReleaseAsync([(riceId, 5m), (chickenId, 3m)], pin);

        var itemId = (await PosApiClient.ReadAsync<MenuItemResponse>(
            await Client.CreateMenuItemAsync("Chicken Fried Rice", "Rice & Curry", 850m))).Id;
        await Client.UpsertRecipeAsync(itemId, (riceId, 0.25m), (chickenId, 0.15m));

        var response = await Client.ConsumeStockAsync(itemId, 2);

        var result = await PosApiClient.ReadAsync<ConsumptionResultResponse>(response);
        result.Deducted.Should().BeTrue();
        result.Lines.Should().Contain(l => l.RawMaterialId == riceId && l.QuantityDeducted == 0.5m);
        result.Lines.Should().Contain(l => l.RawMaterialId == chickenId && l.QuantityDeducted == 0.3m);

        var kitchen = await ReadStockAsync(await Client.GetKitchenStockAsync());
        kitchen.Should().Contain(s => s.RawMaterialId == riceId && s.QuantityOnHand == 4.5m);
        kitchen.Should().Contain(s => s.RawMaterialId == chickenId && s.QuantityOnHand == 2.7m);
    }

    [Fact]
    public async Task ConsumingStock_ForAMenuItemWithNoRecipe_SucceedsAsANoOp()
    {
        await SignInAsAdminAsync();
        var itemId = (await PosApiClient.ReadAsync<MenuItemResponse>(
            await Client.CreateMenuItemAsync("Bottled Water", "Beverages", 100m))).Id;

        var response = await Client.ConsumeStockAsync(itemId, 5);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await PosApiClient.ReadAsync<ConsumptionResultResponse>(response)).Deducted.Should().BeFalse();
    }

    [Fact]
    public async Task ConsumingStock_ForADisabledRecipe_SucceedsAsANoOpRatherThanFailing()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var itemId = (await PosApiClient.ReadAsync<MenuItemResponse>(
            await Client.CreateMenuItemAsync("Fried Rice", "Rice", 700m))).Id;
        await Client.UpsertRecipeAsync(itemId, (riceId, 0.25m));
        await Client.SetRecipeEnabledAsync(itemId, false);

        var response = await Client.ConsumeStockAsync(itemId, 1);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await PosApiClient.ReadAsync<ConsumptionResultResponse>(response)).Deducted.Should().BeFalse();
    }

    [Fact]
    public async Task ConsumingMoreThanKitchenHasInStock_IsBlocked()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var itemId = (await PosApiClient.ReadAsync<MenuItemResponse>(
            await Client.CreateMenuItemAsync("Fried Rice", "Rice", 700m))).Id;
        await Client.UpsertRecipeAsync(itemId, (riceId, 1m));
        // No stock has been released to the kitchen at all.

        var response = await Client.ConsumeStockAsync(itemId, 1);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Inventory.InsufficientStock");
    }

    [Fact]
    public async Task AnAdjustmentRequiresAReason()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");

        var response = await Client.CreateMainStoreAdjustmentAsync(riceId, 5m, "");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AnAdjustmentCorrectsTheBalanceAndIsRecordedInHistory()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var supplierId = await CreateSupplierAsync();
        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 20m)]);

        var response = await Client.CreateMainStoreAdjustmentAsync(riceId, -2m, "Spillage during counting");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stock = await ReadStockAsync(await Client.GetMainStoreStockAsync());
        stock.Should().Contain(s => s.RawMaterialId == riceId && s.QuantityOnHand == 18m);

        var history = await ReadMovementsAsync(await Client.GetMainStoreMovementsAsync());
        history.Should().Contain(m => m.Type == "Adjustment" && m.Notes == "Spillage during counting");
    }

    [Fact]
    public async Task DeactivatingARawMaterial_BlocksItFromANewGoodsReceivedNote()
    {
        await SignInAsAdminAsync();
        var riceId = await CreateRawMaterialAsync("Rice");
        var supplierId = await CreateSupplierAsync();
        await Client.SetRawMaterialActiveAsync(riceId, false);

        var response = await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 5m)]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("RawMaterial.Inactive");
    }

    [Fact]
    public async Task StaffGrantedOnlyKitchenTracking_CannotReceiveGoodsIntoMainStore()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "KitchenStockTracking");

        (await staff.GetMainStoreStockAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.GetKitchenStockAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StaffGrantedOnlyStoreStockManagement_CannotCreateAStockRelease()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "StoreStockManagement");

        var response = await staff.CreateStockReleaseAsync([(Guid.NewGuid(), 1m)], "0000");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> CreateRawMaterialAsync(string name, string unit = "Kilogram", decimal? mainStoreReorderLevel = null) =>
        (await PosApiClient.ReadAsync<RawMaterialResponse>(
            await Client.CreateRawMaterialAsync(name, unit, mainStoreReorderLevel))).Id;

    private async Task<Guid> CreateSupplierAsync(string name = "ABC Wholesale") =>
        (await PosApiClient.ReadAsync<SupplierResponse>(await Client.CreateSupplierAsync(name))).Id;

    private async Task<string> SetApprovalPinAsync() =>
        (await PosApiClient.ReadAsync<PinResponse>(await Client.SetApprovalPinAsync(AdminPassword, "4821"))).Pin;

    private static Task<List<StockLevelResponse>> ReadStockAsync(HttpResponseMessage response) =>
        PosApiClient.ReadAsync<List<StockLevelResponse>>(response);

    private static Task<List<StockMovementResponse>> ReadMovementsAsync(HttpResponseMessage response) =>
        PosApiClient.ReadAsync<List<StockMovementResponse>>(response);
}