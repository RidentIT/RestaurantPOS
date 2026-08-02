using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Authentication.Commands.Logout;

/// <summary>
/// Ends a session by revoking its refresh token. Succeeds even for an unknown token so that
/// signing out is always safe to call and never leaks whether a token was real.
/// </summary>
public sealed record LogoutCommand(string? RefreshToken) : IRequest<Result>;

internal sealed class LogoutCommandHandler(
    IAppDbContext db,
    ITokenService tokens,
    IDateTimeProvider clock)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Success();
        }

        var hash = tokens.HashRefreshToken(request.RefreshToken);

        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == hash), cancellationToken);

        var token = user?.FindActiveRefreshToken(hash, clock.UtcNow);
        if (user is not null && token is not null)
        {
            user.RevokeRefreshToken(token, clock.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}