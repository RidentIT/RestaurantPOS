using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Inventory.Queries.GetRawMaterials;

public sealed record GetRawMaterialsQuery(string? Search, bool? IsActive)
    : IRequest<Result<IReadOnlyCollection<RawMaterialDto>>>;

internal sealed class GetRawMaterialsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetRawMaterialsQuery, Result<IReadOnlyCollection<RawMaterialDto>>>
{
    public async Task<Result<IReadOnlyCollection<RawMaterialDto>>> Handle(
        GetRawMaterialsQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.RawMaterials.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(r => r.Name.ToLower().Contains(term));
        }

        if (request.IsActive is not null)
        {
            query = query.Where(r => r.IsActive == request.IsActive.Value);
        }

        var rawMaterials = await query
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<RawMaterialDto> result = [.. rawMaterials.Select(r => r.ToDto())];

        return Result.Success(result);
    }
}