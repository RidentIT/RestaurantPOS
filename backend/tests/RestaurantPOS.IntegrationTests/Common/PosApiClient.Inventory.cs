using System.Net.Http.Json;

namespace RestaurantPOS.IntegrationTests.Common;

/// <summary>A menu item as the API returns it.</summary>
public sealed record MenuItemResponse(
    Guid Id, string Name, string Category, decimal Price, bool IsActive, bool HasRecipe);

public sealed record RecipeLineResponse(Guid RawMaterialId, string RawMaterialName, string UnitOfMeasurement, decimal Quantity);

public sealed record RecipeResponse(Guid Id, Guid MenuItemId, bool IsEnabled, IReadOnlyCollection<RecipeLineResponse> Lines);

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

    public Task<HttpResponseMessage> CreateMenuItemAsync(string name, string category, decimal price) =>
        Http.PostAsJsonAsync($"{BaseUrl}/menu-items", new { name, category, price }, Json);

    public Task<HttpResponseMessage> UpdateMenuItemAsync(Guid id, string name, string category, decimal price) =>
        Http.PutAsJsonAsync($"{BaseUrl}/menu-items/{id}", new { name, category, price }, Json);

    public Task<HttpResponseMessage> SetMenuItemActiveAsync(Guid id, bool isActive) =>
        Http.PutAsJsonAsync($"{BaseUrl}/menu-items/{id}/status", new { isActive }, Json);

    public Task<HttpResponseMessage> GetMenuItemsAsync(string? query = null) =>
        Http.GetAsync($"{BaseUrl}/menu-items{query}");

    public Task<HttpResponseMessage> GetMenuItemAsync(Guid id) => Http.GetAsync($"{BaseUrl}/menu-items/{id}");

    // ----- Recipes -----

    public Task<HttpResponseMessage> GetRecipeAsync(Guid menuItemId) =>
        Http.GetAsync($"{BaseUrl}/menu-items/{menuItemId}/recipe");

    public Task<HttpResponseMessage> UpsertRecipeAsync(Guid menuItemId, params (Guid RawMaterialId, decimal Quantity)[] lines) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/menu-items/{menuItemId}/recipe",
            new { lines = lines.Select(l => new { rawMaterialId = l.RawMaterialId, quantity = l.Quantity }) },
            Json);

    public Task<HttpResponseMessage> SetRecipeEnabledAsync(Guid menuItemId, bool isEnabled) =>
        Http.PutAsJsonAsync($"{BaseUrl}/menu-items/{menuItemId}/recipe/status", new { isEnabled }, Json);

    public Task<HttpResponseMessage> DeleteRecipeAsync(Guid menuItemId) =>
        Http.DeleteAsync($"{BaseUrl}/menu-items/{menuItemId}/recipe");

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

    public Task<HttpResponseMessage> ConsumeStockAsync(Guid menuItemId, decimal quantitySold) =>
        Http.PostAsJsonAsync($"{BaseUrl}/inventory/kitchen/consumption/{menuItemId}", new { quantitySold }, Json);

    // ----- Releases -----

    public Task<HttpResponseMessage> CreateStockReleaseAsync(
        (Guid RawMaterialId, decimal Quantity)[] lines, string pin, string? notes = null) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/inventory/releases",
            new { lines = lines.Select(l => new { rawMaterialId = l.RawMaterialId, quantity = l.Quantity }), pin, notes },
            Json);

    public Task<HttpResponseMessage> GetStockReleasesAsync() => Http.GetAsync($"{BaseUrl}/inventory/releases");
}