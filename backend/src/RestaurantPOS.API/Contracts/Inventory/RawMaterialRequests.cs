using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Contracts.Inventory;

public sealed record CreateRawMaterialRequest(
    string Name, UnitOfMeasurement UnitOfMeasurement, decimal? MainStoreReorderLevel, decimal? KitchenParLevel);

public sealed record UpdateRawMaterialRequest(
    string Name, UnitOfMeasurement UnitOfMeasurement, decimal? MainStoreReorderLevel, decimal? KitchenParLevel);

public sealed record SetRawMaterialActiveRequest(bool IsActive);