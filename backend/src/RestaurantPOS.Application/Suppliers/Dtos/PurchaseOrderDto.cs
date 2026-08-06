using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Suppliers.Dtos;

public sealed record PurchaseOrderLineDto(
    Guid RawMaterialId,
    string RawMaterialName,
    UnitOfMeasurement UnitOfMeasurement,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>A purchase order's header for a list screen, without its lines.</summary>
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
    int LineCount);

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