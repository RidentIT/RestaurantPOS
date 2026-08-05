using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Suppliers.Queries.GetSuppliers;

public sealed record GetSuppliersQuery(string? Search, bool? IsActive)
    : IRequest<Result<IReadOnlyCollection<SupplierDto>>>;

internal sealed class GetSuppliersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetSuppliersQuery, Result<IReadOnlyCollection<SupplierDto>>>
{
    public async Task<Result<IReadOnlyCollection<SupplierDto>>> Handle(
        GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Suppliers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(s => s.Name.ToLower().Contains(term));
        }

        if (request.IsActive is not null)
        {
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        }

        var suppliers = await query
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<SupplierDto> result = [.. suppliers.Select(s => s.ToDto())];

        return Result.Success(result);
    }
}