using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Authentication.Queries.GetCurrentUser;

/// <summary>
/// Returns the signed-in user's profile and effective module access. The frontend calls this
/// on start-up to rebuild its navigation without trusting anything cached on the client.
/// </summary>
public sealed record GetCurrentUserQuery : IRequest<Result<UserDto>>;

internal sealed class GetCurrentUserQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return Result.Failure<UserDto>(AuthErrors.InvalidCredentials);
        }

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.ModulePermissions)
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        return user is null
            ? Result.Failure<UserDto>(AuthErrors.InvalidCredentials)
            : Result.Success(user.ToDto());
    }
}