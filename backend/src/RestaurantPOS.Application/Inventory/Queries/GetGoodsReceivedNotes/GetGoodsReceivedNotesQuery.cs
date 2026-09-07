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

        var noteIds = notes.Select(n => n.Id).ToList();

        // Every GoodsReceived movement carries a non-null ReferenceId (the GRN's own id), so the
        // group key can be safely unwrapped once nulls are excluded.
        var lines = await db.StockMovements.AsNoTracking()
            .Where(m => m.Type == StockMovementType.GoodsReceived
                && m.ReferenceId != null && noteIds.Contains(m.ReferenceId!.Value))
            .Select(m => new { ReferenceId = m.ReferenceId!.Value, m.RawMaterialId })
            .ToListAsync(cancellationToken);

        var rawMaterialIds = lines.Select(l => l.RawMaterialId).Distinct().ToList();
        var rawMaterialNames = await db.RawMaterials.AsNoTracking()
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var namesByNote = lines
            .GroupBy(l => l.ReferenceId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(l => rawMaterialNames.GetValueOrDefault(l.RawMaterialId, "(unknown)"))
                    .OrderBy(n => n)
                    .ToList());

        IReadOnlyCollection<GoodsReceivedNoteSummaryDto> result =
        [
            .. notes.Select(n =>
            {
                var names = namesByNote.GetValueOrDefault(n.Id, []);

                return new GoodsReceivedNoteSummaryDto(
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
                    names.Count,
                    names);
            }),
        ];

        return Result.Success(result);
    }
}