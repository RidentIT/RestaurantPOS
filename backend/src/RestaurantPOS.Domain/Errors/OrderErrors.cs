using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by table, order, payment and kitchen ticket use cases.</summary>
public static class OrderErrors
{
    public static Error TableNotFound(Guid id) =>
        Error.NotFound("Table.NotFound", $"No table was found with id '{id}'.");

    public static readonly Error TableNumberTaken =
        Error.Conflict("Table.NumberTaken", "A table with that number already exists.");

    public static readonly Error TableInactive =
        Error.Validation("Table.Inactive", "This table is out of service and cannot take an order.");

    public static readonly Error TableOccupied =
        Error.Conflict("Table.Occupied", "This table already has an order in progress.");

    public static readonly Error TableInUse =
        Error.Conflict("Table.InUse", "This table has an order in progress and cannot be taken out of service.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Order.NotFound", $"No order was found with id '{id}'.");

    public static Error ItemNotFound(Guid id) =>
        Error.NotFound("Order.ItemNotFound", $"No item was found on this order with id '{id}'.");

    public static readonly Error NotEditable =
        Error.Conflict("Order.NotEditable", "This order can no longer be changed.");

    public static readonly Error NotDraft =
        Error.Conflict("Order.NotDraft", "Only a draft order can be confirmed.");

    public static readonly Error NoItems =
        Error.Validation("Order.NoItems", "Add at least one item before confirming the order.");

    public static readonly Error NotOpen =
        Error.Conflict("Order.NotOpen", "Only an open order can be checked out.");

    public static readonly Error NotInCheckout =
        Error.Conflict("Order.NotInCheckout", "This order is not being paid for.");

    public static readonly Error NotCancellable =
        Error.Conflict("Order.NotCancellable", "A completed or already-cancelled order cannot be cancelled.");

    public static Error DiscountExceedsSubtotal(decimal subtotal) =>
        Error.Validation(
            "Order.DiscountExceedsSubtotal", $"A discount cannot exceed the order subtotal of {subtotal:0.00}.");

    public static Error PaymentMismatch(decimal total, decimal paid) =>
        Error.Validation(
            "Order.PaymentMismatch",
            $"The payments taken come to {paid:0.00}, but the bill is {total:0.00}. They must match exactly.");

    public static Error MenuItemNotFound(Guid id) =>
        Error.NotFound("Order.MenuItemNotFound", $"No menu item size was found with id '{id}'.");

    public static readonly Error MenuItemInactive =
        Error.Validation("Order.MenuItemInactive", "This menu item is not currently available.");

    public static readonly Error NoReceipt =
        Error.NotFound("Order.NoReceipt", "This order has no receipt because it has not been paid.");

    public static Error TicketNotFound(Guid id) =>
        Error.NotFound("KitchenTicket.NotFound", $"No kitchen ticket was found with id '{id}'.");

    public static Error TicketCannotGoBack(string from, string to) =>
        Error.Conflict("KitchenTicket.CannotGoBack", $"A kitchen ticket cannot move from {from} back to {to}.");
}
