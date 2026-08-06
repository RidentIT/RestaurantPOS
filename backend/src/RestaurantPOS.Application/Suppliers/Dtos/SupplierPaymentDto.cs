using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Suppliers.Dtos;

public sealed record SupplierPaymentDto(
    Guid Id,
    Guid PurchaseOrderId,
    decimal Amount,
    DateTime PaymentDateUtc,
    PaymentMethod Method,
    string? InvoiceReference,
    Guid RecordedByUserId,
    string RecordedByName,
    string? Notes);