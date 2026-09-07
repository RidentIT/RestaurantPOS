using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Inventory.Dtos;

/// <summary>A raw material's current balance in one store.</summary>
public sealed record StockLevelDto(
    Guid RawMaterialId,
    string RawMaterialName,
    UnitOfMeasurement UnitOfMeasurement,
    StoreType Store,
    decimal QuantityOnHand,
    bool IsLowStock);

/// <summary>One row in the permanent stock ledger, with display details denormalised in.</summary>
public sealed record StockMovementDto(
    Guid Id,
    Guid RawMaterialId,
    string RawMaterialName,
    UnitOfMeasurement UnitOfMeasurement,
    StoreType Store,
    decimal QuantityDelta,
    StockMovementType Type,
    Guid? ReferenceId,
    Guid PerformedByUserId,
    string PerformedByName,
    DateTime OccurredAtUtc,
    string? Notes);

/// <summary>A Goods Received Note with its lines reconstructed from the stock ledger.</summary>
public sealed record GoodsReceivedNoteDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    Guid ReceivedByUserId,
    string ReceivedByName,
    DateTime ReceivedAtUtc,
    string? Notes,
    Guid? PurchaseOrderId,
    int? QualityRating,
    bool HasIssue,
    IReadOnlyCollection<StockMovementLineDto> Lines);

/// <summary>A Main Store → Kitchen stock release with its lines reconstructed from the stock ledger.</summary>
public sealed record StockReleaseDto(
    Guid Id,
    Guid RequestedByUserId,
    string RequestedByName,
    DateTime RequestedAtUtc,
    Guid ApprovedByUserId,
    string ApprovedByName,
    DateTime ApprovedAtUtc,
    string? Notes,
    IReadOnlyCollection<StockMovementLineDto> Lines);

/// <summary>One ingredient quantity within a GRN or release, without the ledger bookkeeping fields.</summary>
public sealed record StockMovementLineDto(
    Guid RawMaterialId, string RawMaterialName, UnitOfMeasurement UnitOfMeasurement, decimal Quantity);

/// <summary>A GRN's header for a list screen, with material names but not quantities.</summary>
public sealed record GoodsReceivedNoteSummaryDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    Guid ReceivedByUserId,
    string ReceivedByName,
    DateTime ReceivedAtUtc,
    string? Notes,
    Guid? PurchaseOrderId,
    int? QualityRating,
    bool HasIssue,
    int LineCount,
    IReadOnlyCollection<string> RawMaterialNames);

/// <summary>A stock release's header for a list screen, with material names but not quantities.</summary>
public sealed record StockReleaseSummaryDto(
    Guid Id,
    Guid RequestedByUserId,
    string RequestedByName,
    DateTime RequestedAtUtc,
    Guid ApprovedByUserId,
    string ApprovedByName,
    DateTime ApprovedAtUtc,
    string? Notes,
    int LineCount,
    IReadOnlyCollection<string> RawMaterialNames);

/// <summary>One raw material and quantity deducted when a menu item's recipe was applied to a sale.</summary>
public sealed record ConsumedLineDto(
    Guid RawMaterialId, string RawMaterialName, decimal QuantityDeducted, UnitOfMeasurement UnitOfMeasurement);

/// <summary>
/// The outcome of applying a menu item's recipe to a completed sale. <see cref="Deducted"/> is
/// false — not a failure — when the item simply has no recipe or its recipe is disabled.
/// </summary>
public sealed record ConsumptionResultDto(bool Deducted, IReadOnlyCollection<ConsumedLineDto> Lines);