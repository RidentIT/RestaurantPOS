namespace RestaurantPOS.Application.Suppliers.Dtos;

/// <summary>
/// Delivery and quality statistics for one supplier, derived entirely from their purchase
/// orders and the Goods Received Notes recorded against them — nothing here is separately
/// logged.
/// </summary>
public sealed record SupplierPerformanceDto(
    Guid SupplierId,
    string SupplierName,
    int TotalOrders,
    int DeliveredOrders,
    int OnTimeDeliveries,
    /// <summary>Null when no delivered order had an expected delivery date to compare against.</summary>
    double? OnTimeDeliveryRate,
    /// <summary>Average days from submission to the first GRN recorded against the order. Null with no data.</summary>
    double? AverageDeliveryDays,
    /// <summary>Average of every quality rating recorded on a GRN for this supplier. Null if none were given.</summary>
    double? AverageQualityRating,
    int IssueCount);