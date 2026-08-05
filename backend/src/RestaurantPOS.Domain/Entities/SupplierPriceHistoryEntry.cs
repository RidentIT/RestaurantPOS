using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>One recorded price for a raw material from a supplier, kept forever for historical tracking.</summary>
public sealed class SupplierPriceHistoryEntry : BaseEntity
{
    // EF Core materialisation.
    private SupplierPriceHistoryEntry()
    {
    }

    private SupplierPriceHistoryEntry(
        Guid supplierId, Guid rawMaterialId, decimal price, Guid recordedByUserId, DateTime recordedAtUtc)
    {
        SupplierId = supplierId;
        RawMaterialId = rawMaterialId;
        Price = price;
        RecordedByUserId = recordedByUserId;
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid SupplierId { get; private set; }

    public Guid RawMaterialId { get; private set; }

    public decimal Price { get; private set; }

    public Guid RecordedByUserId { get; private set; }

    public DateTime RecordedAtUtc { get; private set; }

    public static SupplierPriceHistoryEntry Record(
        Guid supplierId, Guid rawMaterialId, decimal price, Guid recordedByUserId, DateTime recordedAtUtc) =>
        new(supplierId, rawMaterialId, price, recordedByUserId, recordedAtUtc);
}