namespace RestaurantPOS.API.Contracts.Suppliers;

public sealed record CreateSupplierRequest(
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    int PaymentTermsDays,
    decimal? CreditLimit,
    int? LeadTimeDays);

public sealed record UpdateSupplierRequest(
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    int PaymentTermsDays,
    decimal? CreditLimit,
    int? LeadTimeDays);

public sealed record SetSupplierActiveRequest(bool IsActive);