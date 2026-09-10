using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Orders.Dtos;

/// <summary>A full bill with its lines, tickets and tenders.</summary>
public sealed record OrderDto(
    Guid Id,
    int? OrderNumber,
    DateOnly? OrderDate,
    Guid? TableId,
    /// <summary>Null for a takeaway order.</summary>
    string? TableNumber,
    OrderStatus Status,
    Guid CashierUserId,
    string CashierName,
    /// <summary>The steward serving the table. Null for a takeaway order or one not yet assigned.</summary>
    Guid? StewardId,
    string? StewardName,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? CompletedAtUtc,
    DiscountType DiscountType,
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
    KitchenTicketStatus? KitchenStatus,
    string? ReceiptNumber,
    IReadOnlyCollection<OrderItemDto> Items,
    IReadOnlyCollection<OrderPaymentDto> Payments);

public sealed record OrderItemDto(
    Guid Id,
    Guid MenuItemVariantId,
    string MenuItemName,
    decimal UnitPrice,
    int Quantity,
    string? SpecialInstructions,
    bool IsCancelled,
    decimal LineTotal);

public sealed record OrderPaymentDto(
    Guid Id,
    OrderPaymentMethod Method,
    decimal Amount,
    decimal? TenderedAmount,
    decimal ChangeGiven,
    string? Reference,
    DateTime CreatedAtUtc);

/// <summary>
/// An order after a change, together with any slip the change obliges the kitchen to be sent.
/// </summary>
/// <remarks>
/// Returned as one object so the till cannot apply a change and forget to print: the response to
/// "add these items" already contains the ticket that has to go out (BR-POS-006). <c>Kot</c> is
/// null when nothing needs printing — an edit to a draft the kitchen has never seen.
/// </remarks>
public sealed record OrderMutationDto(OrderDto Order, KotDocumentDto? Kot);

/// <summary>An order's header for the dashboard and search results, without its lines.</summary>
public sealed record OrderSummaryDto(
    Guid Id,
    int? OrderNumber,
    Guid? TableId,
    /// <summary>Null for a takeaway order.</summary>
    string? TableNumber,
    OrderStatus Status,
    string CashierName,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    int ItemCount,
    decimal Total,
    KitchenTicketStatus? KitchenStatus);
