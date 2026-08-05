using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Queries.GetSupplierPerformance;

/// <summary>
/// Delivery and quality statistics for a supplier. Everything here is derived from purchase
/// orders and the GRNs recorded against them — on-time delivery compares a GRN's received date
/// to its order's expected date, delivery time compares it to when the order was submitted, and
/// quality/issues come straight from what was recorded on each GRN.
/// </summary>
public sealed record GetSupplierPerformanceQuery(Guid SupplierId) : IRequest<Result<SupplierPerformanceDto>>;

internal sealed class GetSupplierPerformanceQueryHandler(IAppDbContext db)
    : IRequestHandler<GetSupplierPerformanceQuery, Result<SupplierPerformanceDto>>
{
    public async Task<Result<SupplierPerformanceDto>> Handle(
        GetSupplierPerformanceQuery request, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);

        if (supplier is null)
        {
            return Result.Failure<SupplierPerformanceDto>(SupplierErrors.NotFound(request.SupplierId));
        }

        var orders = await db.PurchaseOrders.AsNoTracking()
            .Where(o => o.SupplierId == request.SupplierId)
            .Select(o => new { o.Id, o.Status, o.ExpectedDeliveryDate, o.SubmittedAtUtc })
            .ToListAsync(cancellationToken);

        var grns = await db.GoodsReceivedNotes.AsNoTracking()
            .Where(g => g.SupplierId == request.SupplierId)
            .Select(g => new { g.PurchaseOrderId, g.ReceivedAtUtc, g.QualityRating, g.HasIssue })
            .ToListAsync(cancellationToken);

        var deliveredOrders = orders.Where(o => o.Status == PurchaseOrderStatus.Delivered).ToList();

        // A PO can be received across more than one GRN; the earliest is what "when did this
        // order actually arrive" means for both the on-time and delivery-time calculations.
        var earliestReceiptByOrder = grns
            .Where(g => g.PurchaseOrderId is not null)
            .GroupBy(g => g.PurchaseOrderId!.Value)
            .ToDictionary(g => g.Key, g => g.Min(x => x.ReceivedAtUtc));

        var onTimeEligible = deliveredOrders
            .Where(o => o.ExpectedDeliveryDate is not null && earliestReceiptByOrder.ContainsKey(o.Id))
            .ToList();

        var onTimeCount = onTimeEligible
            .Count(o => earliestReceiptByOrder[o.Id].Date <= o.ExpectedDeliveryDate!.Value.Date);

        var deliveryDurations = deliveredOrders
            .Where(o => o.SubmittedAtUtc is not null && earliestReceiptByOrder.ContainsKey(o.Id))
            .Select(o => (earliestReceiptByOrder[o.Id] - o.SubmittedAtUtc!.Value).TotalDays)
            .ToList();

        var ratings = grns.Where(g => g.QualityRating is not null).Select(g => (double)g.QualityRating!.Value).ToList();

        var dto = new SupplierPerformanceDto(
            supplier.Id,
            supplier.Name,
            orders.Count,
            deliveredOrders.Count,
            onTimeCount,
            onTimeEligible.Count == 0 ? null : onTimeCount * 100.0 / onTimeEligible.Count,
            deliveryDurations.Count == 0 ? null : deliveryDurations.Average(),
            ratings.Count == 0 ? null : ratings.Average(),
            grns.Count(g => g.HasIssue));

        return Result.Success(dto);
    }
}