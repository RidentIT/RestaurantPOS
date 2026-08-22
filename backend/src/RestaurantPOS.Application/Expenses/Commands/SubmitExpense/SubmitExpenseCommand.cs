using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.SubmitExpense;

/// <summary>Puts a draft expense forward for a manager to rule on (EXP-015).</summary>
public sealed record SubmitExpenseCommand(Guid ExpenseId, string? Comments) : IRequest<Result<ExpenseDto>>;

public sealed class SubmitExpenseCommandValidator : AbstractValidator<SubmitExpenseCommand>
{
    public SubmitExpenseCommandValidator()
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
        RuleFor(x => x.Comments).MaximumLength(ExpenseApprovalEntry.CommentsMaxLength);
    }
}

internal sealed class SubmitExpenseCommandHandler(
    IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<SubmitExpenseCommand, Result<ExpenseDto>>
{
    public async Task<Result<ExpenseDto>> Handle(SubmitExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await ExpenseRepository.FindAsync(db, request.ExpenseId, cancellationToken);

        if (expense is null)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.NotFound(request.ExpenseId));
        }

        if (expense.Status != ExpenseStatus.Draft)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.NotSubmittable);
        }

        expense.Submit(currentUser.UserId!.Value, clock.UtcNow, request.Comments);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await ExpenseResultFactory.BuildAsync(db, expense, cancellationToken));
    }
}
