using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Suppliers.Queries.GetSupplierPrices;

/// <summary>
/// Current supplier prices, filterable by supplier (a supplier's whole price list) or by raw
/// material (every supplier's price for one ingredient, for side-by-side comparison) or both.
/// </summary>
public sealed record GetSupplierPricesQuery(Guid? SupplierId, Guid? RawMaterialId)
    : IRequest<Result<IReadOnlyCollection<SupplierPriceDto>>>;

internal sealed class GetSupplierPricesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetSupplierPricesQuery, Result<IReadOnlyCollection<SupplierPriceDto>>>
{
    public async Task<Result<IReadOnlyCollection<SupplierPriceDto>>> Handle(
        GetSupplierPricesQuery request, CancellationToken cancellationToken)
    {
        var query = db.SupplierPrices.AsNoTracking().AsQueryable();

        if (request.SupplierId is not null)
        {
            query = query.Where(p => p.SupplierId == request.SupplierId.Value);
        }

        if (request.RawMaterialId is not null)
        {
            query = query.Where(p => p.RawMaterialId == request.RawMaterialId.Value);
        }

        var prices = await query.ToListAsync(cancellationToken);

        var supplierNames = await db.Suppliers.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var rawMaterials = await db.RawMaterials.AsNoTracking()
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        IReadOnlyCollection<SupplierPriceDto> result =
        [
            .. prices
                .Select(p => new SupplierPriceDto(
                    p.SupplierId,
                    supplierNames.GetValueOrDefault(p.SupplierId, "(unknown)"),
                    p.RawMaterialId,
                    rawMaterials.TryGetValue(p.RawMaterialId, out var material) ? material.Name : "(unknown)",
                    rawMaterials.TryGetValue(p.RawMaterialId, out var m2) ? m2.UnitOfMeasurement : default,
                    p.Price,
                    p.UpdatedAtUtc))
                .OrderBy(p => p.RawMaterialName)
                .ThenBy(p => p.Price),
        ];

        return Result.Success(result);
    }
}