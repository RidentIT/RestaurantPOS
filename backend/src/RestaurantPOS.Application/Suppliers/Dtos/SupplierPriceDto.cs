using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Suppliers.Dtos;

public sealed record SupplierPriceDto(
    Guid SupplierId,
    string SupplierName,
    Guid RawMaterialId,
    string RawMaterialName,
    UnitOfMeasurement UnitOfMeasurement,
    decimal Price,
    DateTime UpdatedAtUtc);

public sealed record SupplierPriceHistoryEntryDto(
    decimal Price, Guid RecordedByUserId, string RecordedByName, DateTime RecordedAtUtc);