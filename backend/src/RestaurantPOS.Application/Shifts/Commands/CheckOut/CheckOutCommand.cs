using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Shifts.Common;
using RestaurantPOS.Application.Shifts.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Shifts.Commands.CheckOut;

/// <summary>
/// Ends the signed-in cashier's shift, recording the cash they counted. Refused while any order
/// opened in the shift is still unpaid, so a bill is never left straddling two shifts.
/// </summary>
public sealed record CheckOutCommand(decimal ClosingCash, string? Note) : IRequest<Result<ShiftDto>>;

public sealed class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutCommandValidator()
    {
        RuleFor(x => x.ClosingCash).GreaterThanOrEqualTo(0).WithMessage("Counted cash cannot be negative.");
        RuleFor(x => x.Note).MaximumLength(CashierShift.NoteMaxLength);
    }
}

internal sealed class CheckOutCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<CheckOutCommand, Result<ShiftDto>>
{
    public async Task<Result<ShiftDto>> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var shift = await ShiftReadModel.FindOpenShiftAsync(db, userId, cancellationToken);

        if (shift is null)
        {
            return Result.Failure<ShiftDto>(ShiftErrors.NoOpenShift);
        }

        var unsettled = await ShiftReadModel.UnsettledOrderCountAsync(db, shift.Id, cancellationToken);

        if (unsettled > 0)
        {
            return Result.Failure<ShiftDto>(ShiftErrors.HasOpenOrders(unsettled));
        }

        var expected = shift.OpeningCash + await ShiftReadModel.CashTakenAsync(db, shift.Id, cancellationToken);

        shift.Close(request.ClosingCash, expected, request.Note, userId, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success((await ShiftReadModel.BuildAsync(db, [shift], cancellationToken)).Single());
    }
}
