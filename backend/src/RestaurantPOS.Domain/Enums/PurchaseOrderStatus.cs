namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// Lifecycle of a Purchase Order. Lines are editable only in <see cref="Draft"/>; every other
/// status represents a commitment that has already left the building (sent to the supplier,
/// confirmed by them, or delivered) and so is no longer freely editable.
/// </summary>
public enum PurchaseOrderStatus
{
    Draft = 1,
    Submitted = 2,
    Confirmed = 3,
    Delivered = 4,
    Cancelled = 5,
}