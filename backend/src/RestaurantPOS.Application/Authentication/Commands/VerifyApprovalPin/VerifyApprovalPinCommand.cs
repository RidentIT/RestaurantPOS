using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Authentication.Commands.VerifyApprovalPin;

/// <summary>
/// Checks a 4-digit PIN against every active administrator, identifying who authorised the
/// action. This is the shared approval gate other modules call — for example, an order
/// cancellation at the till prompts for a PIN and records the approving administrator.
/// </summary>
/// <param name="Pin">The PIN typed by the administrator standing at the till.</param>
/// <param name="Reason">Free text describing what is being approved, for the audit trail.</param>
public sealed record VerifyApprovalPinCommand(string Pin, string? Reason)
    : IRequest<Result<ApprovalResult>>;

/// <summary>Identifies the administrator whose PIN authorised an action.</summary>
public sealed record ApprovalResult(Guid ApprovedByUserId, string ApprovedByName, DateTime ApprovedAtUtc);

public sealed class VerifyApprovalPinCommandValidator : AbstractValidator<VerifyApprovalPinCommand>
{
    public VerifyApprovalPinCommandValidator() =>
        RuleFor(x => x.Pin)
            .Must(pin => !string.IsNullOrEmpty(pin)
                && pin.Length == User.ApprovalPinLength
                && pin.All(char.IsAsciiDigit))
            .WithMessage($"The approval PIN must be exactly {User.ApprovalPinLength} digits.");
}

internal sealed class VerifyApprovalPinCommandHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IDateTimeProvider clock)
    : IRequestHandler<VerifyApprovalPinCommand, Result<ApprovalResult>>
{
    public async Task<Result<ApprovalResult>> Handle(
        VerifyApprovalPinCommand request,
        CancellationToken cancellationToken)
    {
        var admins = await db.Users
            .Where(u => u.IsActive && u.Role == UserRole.Admin && u.ApprovalPinHash != null)
            .Select(u => new { u.Id, u.FullName, u.ApprovalPinHash })
            .ToListAsync(cancellationToken);

        if (admins.Count == 0)
        {
            return Result.Failure<ApprovalResult>(AuthErrors.NoAdminPinConfigured);
        }

        // Every candidate is checked rather than breaking on the first match, so the time taken
        // does not reveal which administrator's PIN was supplied.
        var matched = admins.Aggregate(
            (Id: Guid.Empty, Name: string.Empty, Found: false),
            (acc, admin) => passwordHasher.Verify(request.Pin, admin.ApprovalPinHash!)
                ? (admin.Id, admin.FullName, true)
                : acc);

        return matched.Found
            ? Result.Success(new ApprovalResult(matched.Id, matched.Name, clock.UtcNow))
            : Result.Failure<ApprovalResult>(AuthErrors.InvalidPin);
    }
}