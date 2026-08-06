using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Orders.Dtos;

/// <summary>
/// What a table looks like on the floor plan. Occupancy and kitchen progress are derived from the
/// table's live order rather than stored, so this can never disagree with the order itself.
/// </summary>
public sealed record TableDto(
    Guid Id,
    string Number,
    int Seats,
    string? Notes,
    bool IsActive,
    /// <summary>Null when the table is free.</summary>
    TableOrderSummaryDto? CurrentOrder);

/// <summary>The live order sitting on a table, as much of it as the floor plan needs.</summary>
public sealed record TableOrderSummaryDto(
    Guid OrderId,
    int? OrderNumber,
    OrderStatus Status,
    int ItemCount,
    decimal Total,
    DateTime? ConfirmedAtUtc,
    string CashierName,
    /// <summary>
    /// How far the kitchen has got overall: the least-advanced ticket still outstanding, so a
    /// table with one dish plated and one still queued reads as preparing, not ready. Null while
    /// the order is a draft and nothing has been sent.
    /// </summary>
    KitchenTicketStatus? KitchenStatus);
