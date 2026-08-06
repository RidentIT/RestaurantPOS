namespace RestaurantPOS.API.Contracts.Suppliers;

public sealed record PurchaseOrderLineRequest(Guid RawMaterialId, decimal Quantity, decimal UnitPrice);

public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId,
    IReadOnlyCollection<PurchaseOrderLineRequest> Lines,
    DateTime? ExpectedDeliveryDate,
    string? Notes);

public sealed record UpdatePurchaseOrderRequest(
    IReadOnlyCollection<PurchaseOrderLineRequest> Lines,
    DateTime? ExpectedDeliveryDate,
    string? Notes);