namespace RestaurantPOS.Domain.Entities;

/// <summary>One raw material, quantity and unit price ordered on a Purchase Order.</summary>
public sealed class PurchaseOrderLine
{
    // EF Core materialisation.
    private PurchaseOrderLine()
    {
    }

    internal PurchaseOrderLine(Guid purchaseOrderId, Guid rawMaterialId, decimal quantity, decimal unitPrice)
    {
        PurchaseOrderId = purchaseOrderId;
        RawMaterialId = rawMaterialId;
        Quantity = quantity > 0
            ? quantity
            : throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero.");
        UnitPrice = unitPrice >= 0
            ? unitPrice
            : throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Unit price cannot be negative.");
    }

    public Guid PurchaseOrderId { get; private set; }

    public Guid RawMaterialId { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;
}