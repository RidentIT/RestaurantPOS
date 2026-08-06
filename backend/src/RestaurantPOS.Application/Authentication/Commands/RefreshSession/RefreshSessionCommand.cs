using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Authentication.Common;
using RestaurantPOS.Application.Authentication.Dtos;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Authentication.Commands.RefreshSession;

/// <summary>Trades a valid refresh token for a new access/refresh pair.</summary>
public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<Result<AuthenticationResult>>;

public sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator() =>
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("A refresh token is required.");
}

internal sealed class RefreshSessionCommandHandler(
    IAppDbContext db,
    ITokenService tokens,
    IDateTimeProvider clock)
    : IRequestHandler<RefreshSessionCommand, Result<AuthenticationResult>>
{
    public async Task<Result<AuthenticationResult>> Handle(
        RefreshSessionCommand request,
        CancellationToken cancellationToken)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var now = clock.UtcNow;

        var user = await db.Users
            .Include(u => u.ModulePermissions)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == hash), cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthenticationResult>(AuthErrors.InvalidRefreshToken);
        }

        var existing = user.FindActiveRefreshToken(hash, now);
        if (existing is null)
        {
            return Result.Failure<AuthenticationResult>(AuthErrors.InvalidRefreshToken);
        }

        if (!user.IsActive)
        {
            // Revoke outstanding sessions so a deactivated account cannot keep refreshing.
            user.RevokeAllRefreshTokens(now);
            await db.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthenticationResult>(AuthErrors.AccountDeactivated);
        }

        // Rotation: the presented token is burned as the replacement is issued.
        user.RevokeRefreshToken(existing, now);
        var session = SessionFactory.Issue(user, tokens, clock);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(session);
    }
}