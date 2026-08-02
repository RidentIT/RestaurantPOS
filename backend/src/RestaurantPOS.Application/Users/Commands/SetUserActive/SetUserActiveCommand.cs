using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Users.Commands.SetUserActive;

/// <summary>
/// Enables or disables a staff account. Accounts are deactivated rather than deleted so that
/// historical orders, bills and stock movements keep pointing at a real user.
/// </summary>
public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest<Result<UserDto>>;

internal sealed class SetUserActiveCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : IRequestHandler<SetUserActiveCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.ModulePermissions)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDto>(UserErrors.NotFound(request.UserId));
        }

        if (user.IsActive == request.IsActive)
        {
            return Result.Success(user.ToDto());
        }

        if (!request.IsActive)
        {
            if (user.Id == currentUser.UserId)
            {
                return Result.Failure<UserDto>(UserErrors.CannotDeactivateSelf);
            }

            if (user.IsSystemAdmin)
            {
                return Result.Failure<UserDto>(UserErrors.CannotModifySystemAdmin);
            }

            if (user.Role == UserRole.Admin)
            {
                var anotherAdmin = await db.Users.AnyAsync(
                    u => u.Id != user.Id && u.IsActive && u.Role == UserRole.Admin,
                    cancellationToken);

                if (!anotherAdmin)
                {
                    return Result.Failure<UserDto>(UserErrors.LastAdmin);
                }
            }

            user.Deactivate();

            // Kill outstanding sessions immediately rather than waiting for the access token
            // to expire on its own.
            user.RevokeAllRefreshTokens(clock.UtcNow);
        }
        else
        {
            user.Activate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(user.ToDto());
    }
}