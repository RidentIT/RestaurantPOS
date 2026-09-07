using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Suppliers.Queries.GetPurchaseOrders;

public sealed record GetPurchaseOrdersQuery(Guid? SupplierId, PurchaseOrderStatus? Status)
    : IRequest<Result<IReadOnlyCollection<PurchaseOrderSummaryDto>>>;

internal sealed class GetPurchaseOrdersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetPurchaseOrdersQuery, Result<IReadOnlyCollection<PurchaseOrderSummaryDto>>>
{
    private const int MaxRows = 300;

    public async Task<Result<IReadOnlyCollection<PurchaseOrderSummaryDto>>> Handle(
        GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = db.PurchaseOrders.AsNoTracking().Include(o => o.Lines).AsQueryable();

        if (request.SupplierId is not null)
        {
            query = query.Where(o => o.SupplierId == request.SupplierId.Value);
        }

        if (request.Status is not null)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var supplierNames = await db.Suppliers.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var rawMaterialIds = orders.SelectMany(o => o.Lines).Select(l => l.RawMaterialId).Distinct().ToList();
        var rawMaterials = await db.RawMaterials.AsNoTracking()
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();
        var paidByOrder = await db.SupplierPayments.AsNoTracking()
            .Where(p => orderIds.Contains(p.PurchaseOrderId))
            .GroupBy(p => p.PurchaseOrderId)
            .Select(g => new { PurchaseOrderId = g.Key, Paid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(g => g.PurchaseOrderId, g => g.Paid, cancellationToken);

        IReadOnlyCollection<PurchaseOrderSummaryDto> result =
        [
            .. orders.Select(o =>
            {
                var paid = paidByOrder.GetValueOrDefault(o.Id, 0m);

                return new PurchaseOrderSummaryDto(
                    o.Id,
                    o.SupplierId,
                    supplierNames.GetValueOrDefault(o.SupplierId, "(unknown)"),
                    o.Status,
                    o.CreatedAtUtc,
                    o.ExpectedDeliveryDate,
                    o.TotalAmount,
                    paid,
                    o.TotalAmount - paid,
                    o.Lines.Count,
                    [
                        .. o.Lines
                            .Where(l => rawMaterials.ContainsKey(l.RawMaterialId))
                            .Select(l => new PurchaseOrderLineDto(
                                l.RawMaterialId,
                                rawMaterials[l.RawMaterialId].Name,
                                rawMaterials[l.RawMaterialId].UnitOfMeasurement,
                                l.Quantity,
                                l.UnitPrice,
                                l.LineTotal))
                            .OrderBy(l => l.RawMaterialName),
                    ]);
            }),
        ];

        return Result.Success(result);
    }
}