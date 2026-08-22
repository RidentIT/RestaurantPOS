using System.Net.Http.Json;

namespace RestaurantPOS.IntegrationTests.Common;

public sealed record TableResponse(
    Guid Id, string Number, int Seats, string? Notes, bool IsActive, TableOrderSummaryResponse? CurrentOrder);

public sealed record TableOrderSummaryResponse(
    Guid OrderId, int? OrderNumber, string Status, int ItemCount, decimal Total, string? KitchenStatus);

public sealed record OrderResponse(
    Guid Id,
    int? OrderNumber,
    Guid TableId,
    string TableNumber,
    string Status,
    string CashierName,
    string DiscountType,
    decimal DiscountValue,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal ServiceChargeRatePercent,
    decimal ServiceChargeAmount,
    decimal TaxRatePercent,
    decimal TaxAmount,
    decimal Total,
    decimal AmountPaid,
    decimal ChangeDue,
    string? KitchenStatus,
    string? ReceiptNumber,
    IReadOnlyCollection<OrderItemResponse> Items,
    IReadOnlyCollection<OrderPaymentResponse> Payments);

public sealed record OrderItemResponse(
    Guid Id, Guid MenuItemId, string MenuItemName, decimal UnitPrice, int Quantity,
    string? SpecialInstructions, bool IsCancelled, decimal LineTotal);

public sealed record OrderPaymentResponse(
    Guid Id, string Method, decimal Amount, decimal? TenderedAmount, decimal ChangeGiven, string? Reference);

public sealed record KotDocumentResponse(
    Guid TicketId, string RestaurantName, int? OrderNumber, string TableNumber, int TicketNumber,
    string Kind, string CashierName, int PrintCount, IReadOnlyCollection<KotLineResponse> Lines);

public sealed record KotLineResponse(string MenuItemName, int Quantity, string? SpecialInstructions, string? Note);

public sealed record OrderMutationResponse(OrderResponse Order, KotDocumentResponse? Kot);

public sealed record ReceiptDocumentResponse(
    string ReceiptNumber,
    string RestaurantName,
    string AddressLine1,
    string? City,
    string? Phone,
    int? OrderNumber,
    string TableNumber,
    string CashierName,
    int PrintCount,
    IReadOnlyCollection<ReceiptLineResponse> Lines,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal ServiceChargeAmount,
    decimal TaxAmount,
    decimal Total,
    decimal ChangeGiven,
    IReadOnlyCollection<OrderPaymentResponse> Payments,
    string QrPayload,
    string FooterMessage);

public sealed record ReceiptLineResponse(string MenuItemName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderSummaryResponse(
    Guid Id, int? OrderNumber, string TableNumber, string Status, int ItemCount, decimal Total);

public sealed record KitchenTicketResponse(
    Guid Id, Guid OrderId, int? OrderNumber, string TableNumber, int TicketNumber,
    string Kind, string Status, int PrintCount, int WaitingMinutes,
    IReadOnlyCollection<KotLineResponse> Lines);

public sealed partial class PosApiClient
{
    public Task<HttpResponseMessage> GetTablesAsync(bool? isActive = null) =>
        Http.GetAsync($"{BaseUrl}/tables{(isActive.HasValue ? $"?isActive={isActive}" : string.Empty)}");

    public Task<HttpResponseMessage> CreateTableAsync(string number, int seats = 4, string? notes = null) =>
        Http.PostAsJsonAsync($"{BaseUrl}/tables", new { number, seats, notes }, Json);

    public Task<HttpResponseMessage> UpdateTableAsync(Guid id, string number, int seats, string? notes = null) =>
        Http.PutAsJsonAsync($"{BaseUrl}/tables/{id}", new { number, seats, notes }, Json);

    public Task<HttpResponseMessage> SetTableActiveAsync(Guid id, bool isActive) =>
        Http.PutAsJsonAsync($"{BaseUrl}/tables/{id}/status", new { isActive }, Json);

    public Task<HttpResponseMessage> CreateOrderAsync(Guid tableId) =>
        Http.PostAsJsonAsync($"{BaseUrl}/orders", new { tableId }, Json);

    public Task<HttpResponseMessage> GetOrdersAsync(string? query = null) =>
        Http.GetAsync($"{BaseUrl}/orders{query}");

    public Task<HttpResponseMessage> GetOrderAsync(Guid id) => Http.GetAsync($"{BaseUrl}/orders/{id}");

    public Task<HttpResponseMessage> AddOrderItemsAsync(
        Guid orderId, params (Guid MenuItemId, int Quantity, string? Instructions)[] items) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/orders/{orderId}/items",
            new { items = items.Select(i => new { menuItemId = i.MenuItemId, quantity = i.Quantity, specialInstructions = i.Instructions }) },
            Json);

    public Task<HttpResponseMessage> ConfirmOrderAsync(Guid orderId) =>
        Http.PostAsync($"{BaseUrl}/orders/{orderId}/confirm", null);

    public Task<HttpResponseMessage> ChangeOrderItemQuantityAsync(
        Guid orderId, Guid itemId, int quantity, string? pin = null) =>
        Http.PutAsJsonAsync($"{BaseUrl}/orders/{orderId}/items/{itemId}/quantity", new { quantity, pin }, Json);

    public Task<HttpResponseMessage> VoidOrderItemAsync(Guid orderId, Guid itemId, string? pin = null) =>
        Http.PostAsJsonAsync($"{BaseUrl}/orders/{orderId}/items/{itemId}/void", new { pin }, Json);

    public Task<HttpResponseMessage> CancelOrderAsync(Guid orderId, string? pin = null, string? reason = null) =>
        Http.PostAsJsonAsync($"{BaseUrl}/orders/{orderId}/cancel", new { pin, reason }, Json);

    public Task<HttpResponseMessage> SetOrderDiscountAsync(Guid orderId, string type, decimal value) =>
        Http.PutAsJsonAsync($"{BaseUrl}/orders/{orderId}/discount", new { type, value }, Json);

    public Task<HttpResponseMessage> StartCheckoutAsync(Guid orderId) =>
        Http.PostAsync($"{BaseUrl}/orders/{orderId}/checkout", null);

    public Task<HttpResponseMessage> ReopenOrderAsync(Guid orderId) =>
        Http.PostAsync($"{BaseUrl}/orders/{orderId}/reopen", null);

    public Task<HttpResponseMessage> PayOrderAsync(
        Guid orderId, params (string Method, decimal Amount, decimal? Tendered)[] payments) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/orders/{orderId}/payments",
            new { payments = payments.Select(p => new { method = p.Method, amount = p.Amount, tenderedAmount = p.Tendered, reference = (string?)null }) },
            Json);

    public Task<HttpResponseMessage> ReprintReceiptAsync(Guid orderId) =>
        Http.PostAsync($"{BaseUrl}/orders/{orderId}/receipt/reprint", null);

    public Task<HttpResponseMessage> GetKitchenTicketsAsync(bool includeServed = false) =>
        Http.GetAsync($"{BaseUrl}/kitchen/tickets?includeServed={includeServed}");

    public Task<HttpResponseMessage> AdvanceKitchenTicketAsync(Guid ticketId, string status) =>
        Http.PutAsJsonAsync($"{BaseUrl}/kitchen/tickets/{ticketId}/status", new { status }, Json);

    public Task<HttpResponseMessage> ReprintKitchenTicketAsync(Guid ticketId) =>
        Http.PostAsync($"{BaseUrl}/kitchen/tickets/{ticketId}/reprint", null);
}
