namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// The current price a supplier charges for a raw material. This is a derived "latest" view,
/// kept in step by the application layer alongside a <see cref="SupplierPriceHistoryEntry"/> that
/// records every change — the same current-value-plus-ledger shape as
/// <see cref="StockLevel"/>/<see cref="StockMovement"/>.
/// </summary>
public sealed class SupplierPrice
{
    // EF Core materialisation.
    private SupplierPrice()
    {
    }

    public SupplierPrice(Guid supplierId, Guid rawMaterialId, decimal price, DateTime updatedAtUtc)
    {
        SupplierId = supplierId;
        RawMaterialId = rawMaterialId;
        Price = ValidatePrice(price);
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid SupplierId { get; private set; }

    public Guid RawMaterialId { get; private set; }

    public decimal Price { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdatePrice(decimal price, DateTime nowUtc)
    {
        Price = ValidatePrice(price);
        UpdatedAtUtc = nowUtc;
    }

    private static decimal ValidatePrice(decimal price) =>
        price >= 0 ? price : throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative.");
}