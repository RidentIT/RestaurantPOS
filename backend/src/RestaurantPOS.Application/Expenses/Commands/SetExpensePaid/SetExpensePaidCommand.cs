using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.SetExpensePaid;

/// <summary>
/// Records that the money has actually gone out (EXP-034). Unlike editing, this stays available
/// after approval: a cheque approved on the 15th and cleared on the 20th has not changed as an
/// expense, only in whether the bank has paid it.
/// </summary>
public sealed record SetExpensePaidCommand(Guid ExpenseId, bool IsPaid, DateOnly? PaymentDate)
    : IRequest<Result<ExpenseDto>>;

public sealed class SetExpensePaidCommandValidator : AbstractValidator<SetExpensePaidCommand>
{
    public SetExpensePaidCommandValidator() => RuleFor(x => x.ExpenseId).NotEmpty();
}

internal sealed class SetExpensePaidCommandHandler(IAppDbContext db)
    : IRequestHandler<SetExpensePaidCommand, Result<ExpenseDto>>
{
    public async Task<Result<ExpenseDto>> Handle(SetExpensePaidCommand request, CancellationToken cancellationToken)
    {
        var expense = await ExpenseRepository.FindAsync(db, request.ExpenseId, cancellationToken);

        if (expense is null)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.NotFound(request.ExpenseId));
        }

        expense.SetPaid(request.IsPaid, request.PaymentDate);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await ExpenseResultFactory.BuildAsync(db, expense, cancellationToken));
    }
}
