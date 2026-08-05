using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Contracts.Suppliers;

public sealed record RecordSupplierPaymentRequest(
    decimal Amount,
    DateTime PaymentDateUtc,
    PaymentMethod Method,
    string? InvoiceReference,
    string? Notes);