using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Suppliers.Queries.GetSupplierPriceHistory;

/// <summary>Every price a supplier has been recorded as charging for one raw material, newest first.</summary>
public sealed record GetSupplierPriceHistoryQuery(Guid SupplierId, Guid RawMaterialId)
    : IRequest<Result<IReadOnlyCollection<SupplierPriceHistoryEntryDto>>>;

internal sealed class GetSupplierPriceHistoryQueryHandler(IAppDbContext db)
    : IRequestHandler<GetSupplierPriceHistoryQuery, Result<IReadOnlyCollection<SupplierPriceHistoryEntryDto>>>
{
    public async Task<Result<IReadOnlyCollection<SupplierPriceHistoryEntryDto>>> Handle(
        GetSupplierPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = await db.SupplierPriceHistoryEntries.AsNoTracking()
            .Where(e => e.SupplierId == request.SupplierId && e.RawMaterialId == request.RawMaterialId)
            .OrderByDescending(e => e.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        var userNames = await db.Users.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        IReadOnlyCollection<SupplierPriceHistoryEntryDto> result =
        [
            .. entries.Select(e => new SupplierPriceHistoryEntryDto(
                e.Price, e.RecordedByUserId, userNames.GetValueOrDefault(e.RecordedByUserId, "(unknown)"), e.RecordedAtUtc)),
        ];

        return Result.Success(result);
    }
}