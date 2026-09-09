using System.Net.Http.Json;

namespace RestaurantPOS.IntegrationTests.Common;

/// <summary>One size on a menu item, as the API returns it.</summary>
public sealed record MenuItemVariantResponse(Guid Id, string? Name, decimal Price, bool HasRecipe);

/// <summary>A menu item as the API returns it.</summary>
public sealed record MenuItemResponse(
    Guid Id, string Name, string Category, bool IsActive, IReadOnlyCollection<MenuItemVariantResponse> Variants);

public sealed record RecipeLineResponse(Guid RawMaterialId, string RawMaterialName, string UnitOfMeasurement, decimal Quantity);

public sealed record RecipeResponse(Guid Id, Guid MenuItemVariantId, bool IsEnabled, IReadOnlyCollection<RecipeLineResponse> Lines);

public sealed record RawMaterialResponse(
    Guid Id,
    string Name,
    string UnitOfMeasurement,
    decimal? MainStoreReorderLevel,
    decimal? KitchenParLevel,
    bool IsActive);

public sealed record StockLevelResponse(
    Guid RawMaterialId, string RawMaterialName, string UnitOfMeasurement, string Store, decimal QuantityOnHand, bool IsLowStock);

public sealed record StockMovementResponse(
    Guid Id,
    Guid RawMaterialId,
    string RawMaterialName,
    string Store,
    decimal QuantityDelta,
    string Type,
    Guid? ReferenceId,
    string PerformedByName,
    string? Notes);

public sealed record StockMovementLineResponse(Guid RawMaterialId, string RawMaterialName, decimal Quantity);

public sealed record GoodsReceivedNoteResponse(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string? Notes,
    Guid? PurchaseOrderId,
    int? QualityRating,
    bool HasIssue,
    IReadOnlyCollection<StockMovementLineResponse> Lines);

public sealed record StockReleaseResponse(
    Guid Id, string ApprovedByName, string? Notes, IReadOnlyCollection<StockMovementLineResponse> Lines);

public sealed record ConsumedLineResponse(Guid RawMaterialId, string RawMaterialName, decimal QuantityDeducted);

public sealed record ConsumptionResultResponse(bool Deducted, IReadOnlyCollection<ConsumedLineResponse> Lines);

/// <summary>Recipe Management and Inventory Management calls, split out from the auth/user API surface.</summary>
public sealed partial class PosApiClient
{
    // ----- Menu items -----

    /// <summary>Creates an item with a single, unnamed size — the common case in tests that don't care about sizing.</summary>
    public Task<HttpResponseMessage> CreateMenuItemAsync(string name, string category, decimal price) =>
        CreateMenuItemAsync(name, category, [(Name: (string?)null, Price: price)]);

    public Task<HttpResponseMessage> CreateMenuItemAsync(
        string name, string category, params (string? Name, decimal Price)[] variants) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/menu-items",
            new { name, category, variants = variants.Select(v => new { name = v.Name, price = v.Price }) },
            Json);

    /// <summary>Replaces the item's sizes wholesale with a single, unnamed one at <paramref name="price"/>.</summary>
    public Task<HttpResponseMessage> UpdateMenuItemAsync(Guid id, string name, string category, decimal price) =>
        UpdateMenuItemAsync(id, name, category, [(Id: (Guid?)null, Name: (string?)null, Price: price)]);

    public Task<HttpResponseMessage> UpdateMenuItemAsync(
        Guid id, string name, string category, params (Guid? Id, string? Name, decimal Price)[] variants) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/menu-items/{id}",
            new { name, category, variants = variants.Select(v => new { id = v.Id, name = v.Name, price = v.Price }) },
            Json);

    public Task<HttpResponseMessage> SetMenuItemActiveAsync(Guid id, bool isActive) =>
        Http.PutAsJsonAsync($"{BaseUrl}/menu-items/{id}/status", new { isActive }, Json);

    public Task<HttpResponseMessage> GetMenuItemsAsync(string? query = null) =>
        Http.GetAsync($"{BaseUrl}/menu-items{query}");

    public Task<HttpResponseMessage> GetMenuItemAsync(Guid id) => Http.GetAsync($"{BaseUrl}/menu-items/{id}");

    public Task<HttpResponseMessage> GetMenuCategoriesAsync() => Http.GetAsync($"{BaseUrl}/menu-items/categories");

    public Task<HttpResponseMessage> CreateMenuCategoryAsync(string name) =>
        Http.PostAsJsonAsync($"{BaseUrl}/menu-items/categories", new { name }, Json);

    public Task<HttpResponseMessage> DeleteMenuCategoryAsync(string name) =>
        Http.DeleteAsync($"{BaseUrl}/menu-items/categories?name={Uri.EscapeDataString(name)}");

    // ----- Recipes ----- (one per menu item size, keyed by its MenuItemVariant id)

    public Task<HttpResponseMessage> GetRecipeAsync(Guid menuItemVariantId) =>
        Http.GetAsync($"{BaseUrl}/menu-items/variants/{menuItemVariantId}/recipe");

    public Task<HttpResponseMessage> UpsertRecipeAsync(
        Guid menuItemVariantId, params (Guid RawMaterialId, decimal Quantity)[] lines) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/menu-items/variants/{menuItemVariantId}/recipe",
            new { lines = lines.Select(l => new { rawMaterialId = l.RawMaterialId, quantity = l.Quantity }) },
            Json);

    public Task<HttpResponseMessage> SetRecipeEnabledAsync(Guid menuItemVariantId, bool isEnabled) =>
        Http.PutAsJsonAsync($"{BaseUrl}/menu-items/variants/{menuItemVariantId}/recipe/status", new { isEnabled }, Json);

    public Task<HttpResponseMessage> DeleteRecipeAsync(Guid menuItemVariantId) =>
        Http.DeleteAsync($"{BaseUrl}/menu-items/variants/{menuItemVariantId}/recipe");

    // ----- Raw materials -----

    public Task<HttpResponseMessage> CreateRawMaterialAsync(
        string name, string unitOfMeasurement = "Kilogram", decimal? mainStoreReorderLevel = null, decimal? kitchenParLevel = null) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/raw-materials",
            new { name, unitOfMeasurement, mainStoreReorderLevel, kitchenParLevel },
            Json);

    public Task<HttpResponseMessage> UpdateRawMaterialAsync(
        Guid id, string name, string unitOfMeasurement, decimal? mainStoreReorderLevel, decimal? kitchenParLevel) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/raw-materials/{id}",
            new { name, unitOfMeasurement, mainStoreReorderLevel, kitchenParLevel },
            Json);

    public Task<HttpResponseMessage> SetRawMaterialActiveAsync(Guid id, bool isActive) =>
        Http.PutAsJsonAsync($"{BaseUrl}/raw-materials/{id}/status", new { isActive }, Json);

    public Task<HttpResponseMessage> GetRawMaterialsAsync(string? query = null) =>
        Http.GetAsync($"{BaseUrl}/raw-materials{query}");

    // ----- Main Store -----

    public Task<HttpResponseMessage> GetMainStoreStockAsync(bool lowStockOnly = false) =>
        Http.GetAsync($"{BaseUrl}/inventory/main-store/stock?lowStockOnly={lowStockOnly}");

    public Task<HttpResponseMessage> GetMainStoreMovementsAsync() =>
        Http.GetAsync($"{BaseUrl}/inventory/main-store/movements");

    public Task<HttpResponseMessage> CreateGoodsReceivedNoteAsync(
        Guid supplierId,
        (Guid RawMaterialId, decimal Quantity)[] lines,
        string? notes = null,
        Guid? purchaseOrderId = null,
        int? qualityRating = null,
        bool hasIssue = false) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/inventory/main-store/goods-received",
            new
            {
                supplierId,
                lines = lines.Select(l => new { rawMaterialId = l.RawMaterialId, quantity = l.Quantity }),
                notes,
                purchaseOrderId,
                qualityRating,
                hasIssue,
            },
            Json);

    public Task<HttpResponseMessage> CreateMainStoreAdjustmentAsync(Guid rawMaterialId, decimal quantityDelta, string reason) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/inventory/main-store/adjustments",
            new { rawMaterialId, store = "MainStore", quantityDelta, reason },
            Json);

    // ----- Kitchen -----

    public Task<HttpResponseMessage> GetKitchenStockAsync(bool lowStockOnly = false) =>
        Http.GetAsync($"{BaseUrl}/inventory/kitchen/stock?lowStockOnly={lowStockOnly}");

    public Task<HttpResponseMessage> GetKitchenMovementsAsync() =>
        Http.GetAsync($"{BaseUrl}/inventory/kitchen/movements");

    public Task<HttpResponseMessage> CreateKitchenAdjustmentAsync(Guid rawMaterialId, decimal quantityDelta, string reason) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/inventory/kitchen/adjustments",
            new { rawMaterialId, store = "Kitchen", quantityDelta, reason },
            Json);

    public Task<HttpResponseMessage> ConsumeStockAsync(Guid menuItemVariantId, decimal quantitySold) =>
        Http.PostAsJsonAsync($"{BaseUrl}/inventory/kitchen/consumption/{menuItemVariantId}", new { quantitySold }, Json);

    // ----- Releases -----

    public Task<HttpResponseMessage> CreateStockReleaseAsync(
        (Guid RawMaterialId, decimal Quantity)[] lines, string pin, string? notes = null) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/inventory/releases",
            new { lines = lines.Select(l => new { rawMaterialId = l.RawMaterialId, quantity = l.Quantity }), pin, notes },
            Json);

    public Task<HttpResponseMessage> GetStockReleasesAsync() => Http.GetAsync($"{BaseUrl}/inventory/releases");
}