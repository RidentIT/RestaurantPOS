using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Inventory.Queries.GetStockMovements;

/// <summary>The stock history for a store, optionally narrowed to one raw material or date range.</summary>
public sealed record GetStockMovementsQuery(
    StoreType Store,
    Guid? RawMaterialId,
    DateTime? FromUtc,
    DateTime? ToUtc) : IRequest<Result<IReadOnlyCollection<StockMovementDto>>>;

internal sealed class GetStockMovementsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetStockMovementsQuery, Result<IReadOnlyCollection<StockMovementDto>>>
{
    private const int MaxRows = 500;

    public async Task<Result<IReadOnlyCollection<StockMovementDto>>> Handle(
        GetStockMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.StockMovements.AsNoTracking().Where(m => m.Store == request.Store);

        if (request.RawMaterialId is not null)
        {
            query = query.Where(m => m.RawMaterialId == request.RawMaterialId.Value);
        }

        if (request.FromUtc is not null)
        {
            query = query.Where(m => m.OccurredAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc is not null)
        {
            query = query.Where(m => m.OccurredAtUtc <= request.ToUtc.Value);
        }

        var movements = await query
            .OrderByDescending(m => m.OccurredAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var rawMaterialIds = movements.Select(m => m.RawMaterialId).Distinct().ToList();
        var rawMaterials = await db.RawMaterials.AsNoTracking()
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var userIds = movements.Select(m => m.PerformedByUserId).Distinct().ToList();
        var userNames = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        IReadOnlyCollection<StockMovementDto> result =
        [
            .. movements.Select(m => new StockMovementDto(
                m.Id,
                m.RawMaterialId,
                rawMaterials.TryGetValue(m.RawMaterialId, out var rawMaterial) ? rawMaterial.Name : "(deleted)",
                rawMaterial?.UnitOfMeasurement ?? default,
                m.Store,
                m.QuantityDelta,
                m.Type,
                m.ReferenceId,
                m.PerformedByUserId,
                userNames.GetValueOrDefault(m.PerformedByUserId, "(unknown)"),
                m.OccurredAtUtc,
                m.Notes)),
        ];

        return Result.Success(result);
    }
}