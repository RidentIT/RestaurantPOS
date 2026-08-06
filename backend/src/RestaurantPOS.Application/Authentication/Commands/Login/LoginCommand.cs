using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Authentication.Common;
using RestaurantPOS.Application.Authentication.Dtos;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Authentication.Commands.Login;

/// <summary>Exchanges a username and password for a session.</summary>
public sealed record LoginCommand(string Username, string Password) : IRequest<Result<AuthenticationResult>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

internal sealed class LoginCommandHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    IDateTimeProvider clock)
    : IRequestHandler<LoginCommand, Result<AuthenticationResult>>
{
    public async Task<Result<AuthenticationResult>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(u => u.ModulePermissions)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (user is null)
        {
            // Burn a comparable amount of time so an unknown username is not measurably
            // faster than a wrong password, which would allow username enumeration.
            passwordHasher.Hash(request.Password);
            return Result.Failure<AuthenticationResult>(AuthErrors.InvalidCredentials);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthenticationResult>(AuthErrors.InvalidCredentials);
        }

        // Checked only after the password so a deactivated account is not distinguishable
        // from a wrong password to someone who does not know the credentials.
        if (!user.IsActive)
        {
            return Result.Failure<AuthenticationResult>(AuthErrors.AccountDeactivated);
        }

        user.RecordSuccessfulLogin(clock.UtcNow);
        var session = SessionFactory.Issue(user, tokens, clock);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(session);
    }
}