using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Inventory.Queries.GetStockReleases;

public sealed record GetStockReleasesQuery : IRequest<Result<IReadOnlyCollection<StockReleaseSummaryDto>>>;

internal sealed class GetStockReleasesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetStockReleasesQuery, Result<IReadOnlyCollection<StockReleaseSummaryDto>>>
{
    private const int MaxRows = 200;

    public async Task<Result<IReadOnlyCollection<StockReleaseSummaryDto>>> Handle(
        GetStockReleasesQuery request, CancellationToken cancellationToken)
    {
        var releases = await db.StockReleases.AsNoTracking()
            .OrderByDescending(r => r.RequestedAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var userNames = await db.Users.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var releaseIds = releases.Select(r => r.Id).ToList();

        // Only the Main Store side is read: the Kitchen side of the same release carries the same
        // raw material set, just with the opposite sign, so one side is enough.
        var lines = await db.StockMovements.AsNoTracking()
            .Where(m => m.Type == StockMovementType.StockReleaseOut
                && m.ReferenceId != null && releaseIds.Contains(m.ReferenceId!.Value))
            .Select(m => new { ReferenceId = m.ReferenceId!.Value, m.RawMaterialId })
            .ToListAsync(cancellationToken);

        var rawMaterialIds = lines.Select(l => l.RawMaterialId).Distinct().ToList();
        var rawMaterialNames = await db.RawMaterials.AsNoTracking()
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var namesByRelease = lines
            .GroupBy(l => l.ReferenceId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(l => rawMaterialNames.GetValueOrDefault(l.RawMaterialId, "(unknown)"))
                    .OrderBy(n => n)
                    .ToList());

        IReadOnlyCollection<StockReleaseSummaryDto> result =
        [
            .. releases.Select(r =>
            {
                var names = namesByRelease.GetValueOrDefault(r.Id, []);

                return new StockReleaseSummaryDto(
                    r.Id,
                    r.RequestedByUserId,
                    userNames.GetValueOrDefault(r.RequestedByUserId, "(unknown)"),
                    r.RequestedAtUtc,
                    r.ApprovedByUserId,
                    userNames.GetValueOrDefault(r.ApprovedByUserId, "(unknown)"),
                    r.ApprovedAtUtc,
                    r.Notes,
                    names.Count,
                    names);
            }),
        ];

        return Result.Success(result);
    }
}