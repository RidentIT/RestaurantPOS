using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Contracts.Inventory;

public sealed record StockLineRequest(Guid RawMaterialId, decimal Quantity);

public sealed record CreateGoodsReceivedNoteRequest(
    Guid SupplierId,
    IReadOnlyCollection<StockLineRequest> Lines,
    string? Notes,
    Guid? PurchaseOrderId = null,
    int? QualityRating = null,
    bool HasIssue = false);

/// <summary>
/// Both the raw material quantities and the administrator's approval PIN travel in one request,
/// since a release is created only once it is already approved.
/// </summary>
public sealed record CreateStockReleaseRequest(
    IReadOnlyCollection<StockLineRequest> Lines, string Pin, string? Notes);

public sealed record CreateStockAdjustmentRequest(
    Guid RawMaterialId, StoreType Store, decimal QuantityDelta, string Reason);

public sealed record ConsumeStockRequest(decimal QuantitySold);