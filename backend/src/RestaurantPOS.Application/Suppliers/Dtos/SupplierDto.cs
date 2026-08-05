namespace RestaurantPOS.Application.Suppliers.Dtos;

public sealed record SupplierDto(
    Guid Id,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    int PaymentTermsDays,
    decimal? CreditLimit,
    int? LeadTimeDays,
    bool IsActive,
    DateTime CreatedAtUtc);