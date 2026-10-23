using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Shifts.Common;
using RestaurantPOS.Application.Shifts.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Shifts.Commands.CheckIn;

/// <summary>
/// Starts the signed-in cashier's shift. Every order they take from here to checking out belongs
/// to it, and its totals start from zero.
/// </summary>
/// <param name="OpeningCash">The cash float in the drawer as the shift begins.</param>
public sealed record CheckInCommand(decimal OpeningCash) : IRequest<Result<ShiftDto>>;

public sealed class CheckInCommandValidator : AbstractValidator<CheckInCommand>
{
    public CheckInCommandValidator() =>
        RuleFor(x => x.OpeningCash).GreaterThanOrEqualTo(0).WithMessage("Opening cash cannot be negative.");
}

internal sealed class CheckInCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<CheckInCommand, Result<ShiftDto>>
{
    public async Task<Result<ShiftDto>> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        if (await ShiftReadModel.FindOpenShiftAsync(db, userId, cancellationToken) is not null)
        {
            return Result.Failure<ShiftDto>(ShiftErrors.AlreadyCheckedIn);
        }

        var shift = CashierShift.Start(userId, request.OpeningCash, clock.UtcNow);
        db.CashierShifts.Add(shift);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The one-open-shift-per-cashier index caught a double tap the check above raced past.
            return Result.Failure<ShiftDto>(ShiftErrors.AlreadyCheckedIn);
        }

        return Result.Success((await ShiftReadModel.BuildAsync(db, [shift], cancellationToken)).Single());
    }
}
