using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by supplier, purchase order, pricing and payment use cases.</summary>
public static class SupplierErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Supplier.NotFound", $"No supplier was found with id '{id}'.");

    public static readonly Error NameTaken =
        Error.Conflict("Supplier.NameTaken", "A supplier with that name already exists.");

    public static readonly Error Inactive =
        Error.Validation("Supplier.Inactive", "This supplier is inactive and cannot be used on a new transaction.");

    public static Error PurchaseOrderNotFound(Guid id) =>
        Error.NotFound("PurchaseOrder.NotFound", $"No purchase order was found with id '{id}'.");

    public static readonly Error NotDraft =
        Error.Conflict("PurchaseOrder.NotDraft", "Only a draft purchase order can be edited.");

    public static readonly Error NotSubmittable =
        Error.Conflict("PurchaseOrder.NotSubmittable", "Only a draft purchase order can be submitted.");

    public static readonly Error NotConfirmable =
        Error.Conflict("PurchaseOrder.NotConfirmable", "Only a submitted purchase order can be confirmed.");

    public static readonly Error NotCancellable =
        Error.Conflict(
            "PurchaseOrder.NotCancellable", "A delivered or already-cancelled purchase order cannot be cancelled.");

    public static readonly Error SupplierMismatch =
        Error.Validation(
            "GoodsReceivedNote.SupplierMismatch", "The selected purchase order was not placed with this supplier.");

    public static readonly Error PurchaseOrderCancelled =
        Error.Conflict(
            "GoodsReceivedNote.PurchaseOrderCancelled", "This purchase order was cancelled and cannot receive goods.");

    public static Error PaymentExceedsBalance(decimal balance) =>
        Error.Validation(
            "SupplierPayment.ExceedsBalance",
            $"This payment exceeds the outstanding balance of {balance:0.00} on the purchase order.");
}