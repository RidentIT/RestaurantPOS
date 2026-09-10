using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Users.Queries.GetStewards;

/// <summary>
/// The roster. The admin screen asks for everyone; the order picker asks for active stewards only.
/// </summary>
public sealed record GetStewardsQuery(bool? IsActive) : IRequest<Result<IReadOnlyCollection<StewardDto>>>;

internal sealed class GetStewardsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetStewardsQuery, Result<IReadOnlyCollection<StewardDto>>>
{
    public async Task<Result<IReadOnlyCollection<StewardDto>>> Handle(
        GetStewardsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Stewards.AsNoTracking();

        if (request.IsActive is not null)
        {
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        }

        var stewards = await query
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<StewardDto> result = [.. stewards.Select(s => s.ToDto())];

        return Result.Success(result);
    }
}
