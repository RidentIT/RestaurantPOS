using System.Globalization;
using System.Security.Cryptography;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Authentication.Commands.SetApprovalPin;

/// <summary>
/// Sets the signed-in administrator's 4-digit approval PIN, used to authorise privileged
/// actions taken at the till such as cancelling an order.
/// </summary>
/// <param name="CurrentPassword">Re-authenticates the admin before the PIN is changed.</param>
/// <param name="Pin">A chosen PIN, or null to have the server generate a random one.</param>
public sealed record SetApprovalPinCommand(string CurrentPassword, string? Pin)
    : IRequest<Result<SetApprovalPinResult>>;

/// <summary>
/// The PIN in plaintext. Returned exactly once, at the moment it is set — only the hash is
/// stored, so it cannot be read back afterwards.
/// </summary>
public sealed record SetApprovalPinResult(string Pin);

public sealed class SetApprovalPinCommandValidator : AbstractValidator<SetApprovalPinCommand>
{
    public SetApprovalPinCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Your current password is required.");

        RuleFor(x => x.Pin)
            .Must(pin => pin!.Length == User.ApprovalPinLength && pin.All(char.IsAsciiDigit))
            .When(x => x.Pin is not null)
            .WithMessage($"The approval PIN must be exactly {User.ApprovalPinLength} digits.");
    }
}

internal sealed class SetApprovalPinCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    IDateTimeProvider clock)
    : IRequestHandler<SetApprovalPinCommand, Result<SetApprovalPinResult>>
{
    public async Task<Result<SetApprovalPinResult>> Handle(
        SetApprovalPinCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return Result.Failure<SetApprovalPinResult>(AuthErrors.InvalidCredentials);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<SetApprovalPinResult>(AuthErrors.InvalidCredentials);
        }

        if (user.Role != UserRole.Admin)
        {
            return Result.Failure<SetApprovalPinResult>(AuthErrors.PinRequiresAdmin);
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure<SetApprovalPinResult>(AuthErrors.PasswordMismatch);
        }

        var pin = request.Pin ?? GeneratePin();

        user.SetApprovalPin(passwordHasher.Hash(pin), clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new SetApprovalPinResult(pin));
    }

    private static string GeneratePin() =>
        RandomNumberGenerator
            .GetInt32(0, (int)Math.Pow(10, User.ApprovalPinLength))
            .ToString($"D{User.ApprovalPinLength}", CultureInfo.InvariantCulture);
}