using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Mappings;

/// <summary>Projects <see cref="PurchaseOrder"/> aggregates onto their read model.</summary>
public static class PurchaseOrderMappings
{
    /// <summary>
    /// Builds the full DTO, denormalising the supplier's and creator's names and each line's raw
    /// material details, and computing the amount paid so far from recorded payments.
    /// </summary>
    public static async Task<PurchaseOrderDto> ToDtoAsync(
        this PurchaseOrder order, IAppDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(db);

        var supplierName = await db.Suppliers.AsNoTracking()
            .Where(s => s.Id == order.SupplierId).Select(s => s.Name).FirstAsync(cancellationToken);

        var createdByName = await db.Users.AsNoTracking()
            .Where(u => u.Id == order.CreatedByUserId).Select(u => u.FullName).FirstAsync(cancellationToken);

        var rawMaterialIds = order.Lines.Select(l => l.RawMaterialId).ToList();
        var rawMaterials = await db.RawMaterials.AsNoTracking()
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var amountPaid = await db.SupplierPayments.AsNoTracking()
            .Where(p => p.PurchaseOrderId == order.Id)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var lines = order.Lines
            .Select(l => new PurchaseOrderLineDto(
                l.RawMaterialId,
                rawMaterials[l.RawMaterialId].Name,
                rawMaterials[l.RawMaterialId].UnitOfMeasurement,
                l.Quantity,
                l.UnitPrice,
                l.LineTotal))
            .OrderBy(l => l.RawMaterialName)
            .ToList();

        return new PurchaseOrderDto(
            order.Id,
            order.SupplierId,
            supplierName,
            order.Status,
            order.CreatedByUserId,
            createdByName,
            order.CreatedAtUtc,
            order.ExpectedDeliveryDate,
            order.SubmittedAtUtc,
            order.Notes,
            order.TotalAmount,
            amountPaid,
            order.TotalAmount - amountPaid,
            lines);
    }
}