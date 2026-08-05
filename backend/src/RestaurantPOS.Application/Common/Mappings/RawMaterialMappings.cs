using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Mappings;

public static class RawMaterialMappings
{
    public static RawMaterialDto ToDto(this RawMaterial rawMaterial)
    {
        ArgumentNullException.ThrowIfNull(rawMaterial);

        return new RawMaterialDto(
            rawMaterial.Id,
            rawMaterial.Name,
            rawMaterial.UnitOfMeasurement,
            rawMaterial.MainStoreReorderLevel,
            rawMaterial.KitchenParLevel,
            rawMaterial.IsActive,
            rawMaterial.CreatedAtUtc);
    }
}