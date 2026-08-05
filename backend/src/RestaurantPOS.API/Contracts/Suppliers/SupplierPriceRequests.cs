namespace RestaurantPOS.API.Contracts.Suppliers;

public sealed record SetSupplierPriceRequest(Guid RawMaterialId, decimal Price);