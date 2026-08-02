using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Users.Queries.GetUserById;

/// <summary>Loads a single staff account for the edit screen.</summary>
public sealed record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDto>>;

internal sealed class GetUserByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.ModulePermissions)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        return user is null
            ? Result.Failure<UserDto>(UserErrors.NotFound(request.UserId))
            : Result.Success(user.ToDto());
    }
}