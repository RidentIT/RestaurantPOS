using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Inventory.Queries.GetStockReleaseById;

public sealed record GetStockReleaseByIdQuery(Guid StockReleaseId) : IRequest<Result<StockReleaseDto>>;

internal sealed class GetStockReleaseByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetStockReleaseByIdQuery, Result<StockReleaseDto>>
{
    public async Task<Result<StockReleaseDto>> Handle(
        GetStockReleaseByIdQuery request, CancellationToken cancellationToken)
    {
        var release = await db.StockReleases.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.StockReleaseId, cancellationToken);

        if (release is null)
        {
            return Result.Failure<StockReleaseDto>(InventoryErrors.StockReleaseNotFound(request.StockReleaseId));
        }

        var requestedByName = await db.Users.AsNoTracking().Where(u => u.Id == release.RequestedByUserId)
            .Select(u => u.FullName).FirstAsync(cancellationToken);
        var approvedByName = await db.Users.AsNoTracking().Where(u => u.Id == release.ApprovedByUserId)
            .Select(u => u.FullName).FirstAsync(cancellationToken);

        // Only the Main Store side is read: it carries the same raw material set and magnitude
        // as the Kitchen side, just with the opposite sign, so one side is enough to rebuild the lines.
        var movements = await db.StockMovements.AsNoTracking()
            .Where(m => m.ReferenceId == release.Id && m.Type == StockMovementType.StockReleaseOut)
            .ToListAsync(cancellationToken);

        var rawMaterialIds = movements.Select(m => m.RawMaterialId).ToList();
        var rawMaterials = await db.RawMaterials.AsNoTracking()
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var lines = movements
            .Select(m => new StockMovementLineDto(
                m.RawMaterialId, rawMaterials[m.RawMaterialId].Name, rawMaterials[m.RawMaterialId].UnitOfMeasurement, -m.QuantityDelta))
            .OrderBy(l => l.RawMaterialName)
            .ToList();

        return Result.Success(new StockReleaseDto(
            release.Id, release.RequestedByUserId, requestedByName, release.RequestedAtUtc,
            release.ApprovedByUserId, approvedByName, release.ApprovedAtUtc, release.Notes, lines));
    }
}