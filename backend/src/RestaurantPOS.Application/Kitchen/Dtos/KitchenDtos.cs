using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Kitchen.Dtos;

/// <summary>One card on the kitchen display.</summary>
public sealed record KitchenTicketDto(
    Guid Id,
    Guid OrderId,
    int? OrderNumber,
    /// <summary>Null for a takeaway order.</summary>
    string? TableNumber,
    int TicketNumber,
    KitchenTicketKind Kind,
    KitchenTicketStatus Status,
    DateTime PrintedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? ReadyAtUtc,
    DateTime? ServedAtUtc,
    int PrintCount,
    /// <summary>Minutes since the slip was printed — what tells the kitchen what is going cold.</summary>
    int WaitingMinutes,
    IReadOnlyCollection<KitchenTicketLineDto> Lines);

public sealed record KitchenTicketLineDto(
    Guid OrderItemId,
    string MenuItemName,
    int Quantity,
    string? SpecialInstructions,
    string? Note);
