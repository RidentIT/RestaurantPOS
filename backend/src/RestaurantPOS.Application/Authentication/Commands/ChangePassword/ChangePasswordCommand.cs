using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Authentication.Common;
using RestaurantPOS.Application.Authentication.Dtos;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Security;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Authentication.Commands.ChangePassword;

/// <summary>
/// Changes the signed-in user's own password. Also the screen shown to accounts flagged
/// <c>MustChangePassword</c> — the seeded administrator and anyone an admin has just created
/// or reset.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword)
    : IRequest<Result<AuthenticationResult>>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Your current password is required.");
        RuleFor(x => x.NewPassword).MustMeetPasswordPolicy();
    }
}

internal sealed class ChangePasswordCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    IDateTimeProvider clock)
    : IRequestHandler<ChangePasswordCommand, Result<AuthenticationResult>>
{
    public async Task<Result<AuthenticationResult>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return Result.Failure<AuthenticationResult>(AuthErrors.InvalidCredentials);
        }

        var user = await db.Users
            .Include(u => u.ModulePermissions)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthenticationResult>(AuthErrors.InvalidCredentials);
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure<AuthenticationResult>(AuthErrors.PasswordMismatch);
        }

        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            return Result.Failure<AuthenticationResult>(AuthErrors.PasswordReused);
        }

        user.SetPassword(passwordHasher.Hash(request.NewPassword));

        // Every other session is invalidated, then a fresh one is issued to this caller so the
        // user stays signed in on the device that made the change.
        user.RevokeAllRefreshTokens(clock.UtcNow);
        var session = SessionFactory.Issue(user, tokens, clock);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(session);
    }
}