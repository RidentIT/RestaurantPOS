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

        // Two movement rows (out + in) are written per raw material line, so halve the count to
        // report the number of raw materials released, not the number of ledger rows.
        var lineCounts = await db.StockMovements.AsNoTracking()
            .Where(m => m.Type == StockMovementType.StockReleaseOut && m.ReferenceId != null)
            .GroupBy(m => m.ReferenceId!.Value)
            .Select(g => new { ReferenceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ReferenceId, g => g.Count, cancellationToken);

        IReadOnlyCollection<StockReleaseSummaryDto> result =
        [
            .. releases.Select(r => new StockReleaseSummaryDto(
                r.Id,
                r.RequestedByUserId,
                userNames.GetValueOrDefault(r.RequestedByUserId, "(unknown)"),
                r.RequestedAtUtc,
                r.ApprovedByUserId,
                userNames.GetValueOrDefault(r.ApprovedByUserId, "(unknown)"),
                r.ApprovedAtUtc,
                r.Notes,
                lineCounts.GetValueOrDefault(r.Id, 0))),
        ];

        return Result.Success(result);
    }
}