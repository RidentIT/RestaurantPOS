using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Security;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Users.Commands.ResetUserPassword;

/// <summary>
/// Sets a temporary password on another user's behalf. The user must choose their own
/// password the next time they sign in.
/// </summary>
public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest<Result>;

public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPassword).MustMeetPasswordPolicy();
    }
}

internal sealed class ResetUserPasswordCommandHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IDateTimeProvider clock)
    : IRequestHandler<ResetUserPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        user.ResetPassword(passwordHasher.Hash(request.NewPassword));

        // Anyone signed in as this user is pushed back to the login screen.
        user.RevokeAllRefreshTokens(clock.UtcNow);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}