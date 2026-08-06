using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Inventory.Dtos;

public sealed record RawMaterialDto(
    Guid Id,
    string Name,
    UnitOfMeasurement UnitOfMeasurement,
    decimal? MainStoreReorderLevel,
    decimal? KitchenParLevel,
    bool IsActive,
    DateTime CreatedAtUtc);