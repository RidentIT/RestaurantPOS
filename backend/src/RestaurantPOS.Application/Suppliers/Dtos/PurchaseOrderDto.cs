using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Suppliers.Dtos;

public sealed record PurchaseOrderLineDto(
    Guid RawMaterialId,
    string RawMaterialName,
    UnitOfMeasurement UnitOfMeasurement,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>
/// A purchase order's header for a list screen.
/// </summary>
/// <remarks>
/// Carries its lines as well as the count. Two orders placed with the same supplier on the same
/// day are otherwise indistinguishable in a picker — what actually tells them apart is what was
/// ordered, so the goods-received screen can name the materials rather than showing a row of
/// identical dates. The lines are already loaded to compute the count, so this costs nothing.
/// </remarks>
public sealed record PurchaseOrderSummaryDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    PurchaseOrderStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ExpectedDeliveryDate,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal Balance,
    int LineCount,
    IReadOnlyCollection<PurchaseOrderLineDto> Lines);

public sealed record PurchaseOrderDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    PurchaseOrderStatus Status,
    Guid CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAtUtc,
    DateTime? ExpectedDeliveryDate,
    DateTime? SubmittedAtUtc,
    string? Notes,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal Balance,
    IReadOnlyCollection<PurchaseOrderLineDto> Lines);