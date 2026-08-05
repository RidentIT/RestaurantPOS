using System.Net.Http.Json;

namespace RestaurantPOS.IntegrationTests.Common;

public sealed record SupplierResponse(
    Guid Id,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    int PaymentTermsDays,
    decimal? CreditLimit,
    int? LeadTimeDays,
    bool IsActive);

public sealed record PurchaseOrderLineResponse(Guid RawMaterialId, string RawMaterialName, decimal Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record PurchaseOrderResponse(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ExpectedDeliveryDate,
    DateTime? SubmittedAtUtc,
    string? Notes,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal Balance,
    IReadOnlyCollection<PurchaseOrderLineResponse> Lines);

public sealed record SupplierPriceResponse(Guid SupplierId, string SupplierName, Guid RawMaterialId, string RawMaterialName, decimal Price);

public sealed record SupplierPriceHistoryEntryResponse(decimal Price, string RecordedByName, DateTime RecordedAtUtc);

public sealed record SupplierPaymentResponse(
    Guid Id, Guid PurchaseOrderId, decimal Amount, DateTime PaymentDateUtc, string Method, string? InvoiceReference);

public sealed record SupplierPerformanceResponse(
    Guid SupplierId,
    int TotalOrders,
    int DeliveredOrders,
    int OnTimeDeliveries,
    double? OnTimeDeliveryRate,
    double? AverageDeliveryDays,
    double? AverageQualityRating,
    int IssueCount);

/// <summary>Supplier Management calls: suppliers, purchase orders, pricing, payments and performance.</summary>
public sealed partial class PosApiClient
{
    public Task<HttpResponseMessage> CreateSupplierAsync(
        string name,
        string? contactName = null,
        string? phone = null,
        string? email = null,
        string? address = null,
        int paymentTermsDays = 0,
        decimal? creditLimit = null,
        int? leadTimeDays = null) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/suppliers",
            new { name, contactName, phone, email, address, paymentTermsDays, creditLimit, leadTimeDays },
            Json);

    public Task<HttpResponseMessage> UpdateSupplierAsync(
        Guid id, string name, int paymentTermsDays = 0, decimal? creditLimit = null, int? leadTimeDays = null) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/suppliers/{id}",
            new { name, contactName = (string?)null, phone = (string?)null, email = (string?)null, address = (string?)null, paymentTermsDays, creditLimit, leadTimeDays },
            Json);

    public Task<HttpResponseMessage> SetSupplierActiveAsync(Guid id, bool isActive) =>
        Http.PutAsJsonAsync($"{BaseUrl}/suppliers/{id}/status", new { isActive }, Json);

    public Task<HttpResponseMessage> GetSuppliersAsync(string? query = null) =>
        Http.GetAsync($"{BaseUrl}/suppliers{query}");

    public Task<HttpResponseMessage> CreatePurchaseOrderAsync(
        Guid supplierId,
        (Guid RawMaterialId, decimal Quantity, decimal UnitPrice)[] lines,
        DateTime? expectedDeliveryDate = null,
        string? notes = null) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/purchase-orders",
            new
            {
                supplierId,
                lines = lines.Select(l => new { rawMaterialId = l.RawMaterialId, quantity = l.Quantity, unitPrice = l.UnitPrice }),
                expectedDeliveryDate,
                notes,
            },
            Json);

    public Task<HttpResponseMessage> UpdatePurchaseOrderAsync(
        Guid id, (Guid RawMaterialId, decimal Quantity, decimal UnitPrice)[] lines, DateTime? expectedDeliveryDate = null, string? notes = null) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/purchase-orders/{id}",
            new
            {
                lines = lines.Select(l => new { rawMaterialId = l.RawMaterialId, quantity = l.Quantity, unitPrice = l.UnitPrice }),
                expectedDeliveryDate,
                notes,
            },
            Json);

    public Task<HttpResponseMessage> SubmitPurchaseOrderAsync(Guid id) =>
        Http.PostAsync($"{BaseUrl}/purchase-orders/{id}/submit", null);

    public Task<HttpResponseMessage> ConfirmPurchaseOrderAsync(Guid id) =>
        Http.PostAsync($"{BaseUrl}/purchase-orders/{id}/confirm", null);

    public Task<HttpResponseMessage> CancelPurchaseOrderAsync(Guid id) =>
        Http.PostAsync($"{BaseUrl}/purchase-orders/{id}/cancel", null);

    public Task<HttpResponseMessage> GetPurchaseOrdersAsync(string? query = null) =>
        Http.GetAsync($"{BaseUrl}/purchase-orders{query}");

    public Task<HttpResponseMessage> GetPurchaseOrderAsync(Guid id) => Http.GetAsync($"{BaseUrl}/purchase-orders/{id}");

    public Task<HttpResponseMessage> SetSupplierPriceAsync(Guid supplierId, Guid rawMaterialId, decimal price) =>
        Http.PutAsJsonAsync($"{BaseUrl}/suppliers/{supplierId}/prices", new { rawMaterialId, price }, Json);

    public Task<HttpResponseMessage> GetSupplierPricesAsync(string? query = null) =>
        Http.GetAsync($"{BaseUrl}/suppliers/prices{query}");

    public Task<HttpResponseMessage> GetSupplierPriceHistoryAsync(Guid supplierId, Guid rawMaterialId) =>
        Http.GetAsync($"{BaseUrl}/suppliers/{supplierId}/prices/{rawMaterialId}/history");

    public Task<HttpResponseMessage> RecordSupplierPaymentAsync(
        Guid purchaseOrderId, decimal amount, DateTime paymentDateUtc, string method = "Cash", string? invoiceReference = null) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/purchase-orders/{purchaseOrderId}/payments",
            new { amount, paymentDateUtc, method, invoiceReference, notes = (string?)null },
            Json);

    public Task<HttpResponseMessage> GetPurchaseOrderPaymentsAsync(Guid purchaseOrderId) =>
        Http.GetAsync($"{BaseUrl}/purchase-orders/{purchaseOrderId}/payments");

    public Task<HttpResponseMessage> GetSupplierPerformanceAsync(Guid supplierId) =>
        Http.GetAsync($"{BaseUrl}/suppliers/{supplierId}/performance");
}