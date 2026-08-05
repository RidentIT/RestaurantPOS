using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Inventory.Queries.GetGoodsReceivedNotes;

public sealed record GetGoodsReceivedNotesQuery : IRequest<Result<IReadOnlyCollection<GoodsReceivedNoteSummaryDto>>>;

internal sealed class GetGoodsReceivedNotesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetGoodsReceivedNotesQuery, Result<IReadOnlyCollection<GoodsReceivedNoteSummaryDto>>>
{
    private const int MaxRows = 200;

    public async Task<Result<IReadOnlyCollection<GoodsReceivedNoteSummaryDto>>> Handle(
        GetGoodsReceivedNotesQuery request, CancellationToken cancellationToken)
    {
        var notes = await db.GoodsReceivedNotes.AsNoTracking()
            .OrderByDescending(n => n.ReceivedAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var supplierNames = await db.Suppliers.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var userNames = await db.Users.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        // Every GoodsReceived movement carries a non-null ReferenceId (the GRN's own id), so the
        // group key can be safely unwrapped once nulls are excluded.
        var lineCounts = await db.StockMovements.AsNoTracking()
            .Where(m => m.Type == StockMovementType.GoodsReceived && m.ReferenceId != null)
            .GroupBy(m => m.ReferenceId!.Value)
            .Select(g => new { ReferenceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ReferenceId, g => g.Count, cancellationToken);

        IReadOnlyCollection<GoodsReceivedNoteSummaryDto> result =
        [
            .. notes.Select(n => new GoodsReceivedNoteSummaryDto(
                n.Id,
                n.SupplierId,
                supplierNames.GetValueOrDefault(n.SupplierId, "(unknown)"),
                n.ReceivedByUserId,
                userNames.GetValueOrDefault(n.ReceivedByUserId, "(unknown)"),
                n.ReceivedAtUtc,
                n.Notes,
                n.PurchaseOrderId,
                n.QualityRating,
                n.HasIssue,
                lineCounts.GetValueOrDefault(n.Id, 0))),
        ];

        return Result.Success(result);
    }
}