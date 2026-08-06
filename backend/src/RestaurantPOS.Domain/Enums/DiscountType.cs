namespace RestaurantPOS.Domain.Enums;

/// <summary>How a bill discount is expressed (POS-007).</summary>
public enum DiscountType
{
    None = 0,

    /// <summary>A percentage of the subtotal, 0-100.</summary>
    Percentage = 1,

    /// <summary>A flat amount off, never more than the subtotal (BR-POS-010).</summary>
    Fixed = 2,
}
