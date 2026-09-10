using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Contracts.Orders;

/// <summary>
/// Null <paramref name="TableId"/> opens a takeaway order instead of a dine-in one.
/// <paramref name="StewardId"/> is the steward serving the table, if picked up front; ignored for
/// a takeaway order.
/// </summary>
public sealed record CreateOrderRequest(Guid? TableId, Guid? StewardId);

/// <summary>Credits a dine-in order to a steward, or clears it with a null id.</summary>
public sealed record AssignOrderStewardRequest(Guid? StewardId);

public sealed record OrderItemRequest(Guid MenuItemVariantId, int Quantity, string? SpecialInstructions);

public sealed record AddOrderItemsRequest(IReadOnlyCollection<OrderItemRequest> Items);

/// <summary>
/// The PIN travels with the change rather than being exchanged for a token first, matching how
/// stock releases are approved elsewhere in this system: one request carries both the action and
/// the authority for it, so there is no window where an approval exists unused.
/// </summary>
public sealed record ChangeOrderItemQuantityRequest(int Quantity, string? Pin);

public sealed record RemoveOrderItemRequest(string? Pin);

public sealed record CancelOrderRequest(string? Pin, string? Reason);

public sealed record SetOrderDiscountRequest(DiscountType Type, decimal Value);

public sealed record OrderPaymentRequest(
    OrderPaymentMethod Method, decimal Amount, decimal? TenderedAmount, string? Reference);

public sealed record CompleteOrderPaymentRequest(IReadOnlyCollection<OrderPaymentRequest> Payments);
