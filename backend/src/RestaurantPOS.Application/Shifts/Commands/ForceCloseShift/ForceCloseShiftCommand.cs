using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Shifts.Common;
using RestaurantPOS.Application.Shifts.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Shifts.Commands.ForceCloseShift;

/// <summary>
/// The owner closing a shift on a cashier's behalf — the cashier forgot to check out, or left
/// mid-service. Unlike a cashier's own check-out it doesn't wait for unpaid orders and needs no
/// cash count; the shift is simply closed against what the system expects.
/// </summary>
public sealed record ForceCloseShiftCommand(Guid ShiftId, decimal? ClosingCash, string? Note)
    : IRequest<Result<ShiftDto>>;

public sealed class ForceCloseShiftCommandValidator : AbstractValidator<ForceCloseShiftCommand>
{
    public ForceCloseShiftCommandValidator()
    {
        RuleFor(x => x.ClosingCash).GreaterThanOrEqualTo(0).When(x => x.ClosingCash.HasValue);
        RuleFor(x => x.Note).MaximumLength(CashierShift.NoteMaxLength);
    }
}

internal sealed class ForceCloseShiftCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<ForceCloseShiftCommand, Result<ShiftDto>>
{
    public async Task<Result<ShiftDto>> Handle(ForceCloseShiftCommand request, CancellationToken cancellationToken)
    {
        var shift = await db.CashierShifts.FirstOrDefaultAsync(s => s.Id == request.ShiftId, cancellationToken);

        if (shift is null)
        {
            return Result.Failure<ShiftDto>(ShiftErrors.NotFound(request.ShiftId));
        }

        if (!shift.IsOpen)
        {
            return Result.Failure<ShiftDto>(ShiftErrors.AlreadyClosed);
        }

        var expected = shift.OpeningCash + await ShiftReadModel.CashTakenAsync(db, shift.Id, cancellationToken);

        shift.Close(request.ClosingCash, expected, request.Note, currentUser.UserId!.Value, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success((await ShiftReadModel.BuildAsync(db, [shift], cancellationToken)).Single());
    }
}
