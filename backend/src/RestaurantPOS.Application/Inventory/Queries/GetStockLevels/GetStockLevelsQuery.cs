using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Inventory.Queries.GetStockLevels;

/// <summary>
/// The current stock view for one store. Every active raw material appears, even ones with no
/// movements yet (balance 0) — this is a snapshot of what should be on the shelf, not just a
/// list of things that have happened.
/// </summary>
public sealed record GetStockLevelsQuery(StoreType Store, bool LowStockOnly = false)
    : IRequest<Result<IReadOnlyCollection<StockLevelDto>>>;

internal sealed class GetStockLevelsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetStockLevelsQuery, Result<IReadOnlyCollection<StockLevelDto>>>
{
    public async Task<Result<IReadOnlyCollection<StockLevelDto>>> Handle(
        GetStockLevelsQuery request,
        CancellationToken cancellationToken)
    {
        var rawMaterials = await db.RawMaterials.AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        var balances = await db.StockLevels.AsNoTracking()
            .Where(l => l.Store == request.Store)
            .ToDictionaryAsync(l => l.RawMaterialId, l => l.QuantityOnHand, cancellationToken);

        var rows = rawMaterials.Select(r =>
        {
            var quantity = balances.GetValueOrDefault(r.Id, 0m);
            var threshold = request.Store == StoreType.MainStore ? r.MainStoreReorderLevel : r.KitchenParLevel;
            var isLow = threshold is not null && quantity <= threshold.Value;

            return new StockLevelDto(r.Id, r.Name, r.UnitOfMeasurement, request.Store, quantity, isLow);
        });

        if (request.LowStockOnly)
        {
            rows = rows.Where(r => r.IsLowStock);
        }

        IReadOnlyCollection<StockLevelDto> result = [.. rows.OrderBy(r => r.RawMaterialName)];

        return Result.Success(result);
    }
}