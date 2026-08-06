using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.DeleteExpense;

/// <summary>
/// Discards an expense that was never ruled on — a mis-key, or a bill entered twice. Once
/// approved or rejected it stays on the books for good (BR-EXP-006): a rejected expense is a
/// decision somebody made, and deleting it would erase the reason.
/// </summary>
public sealed record DeleteExpenseCommand(Guid ExpenseId) : IRequest<Result>;

public sealed class DeleteExpenseCommandValidator : AbstractValidator<DeleteExpenseCommand>
{
    public DeleteExpenseCommandValidator() => RuleFor(x => x.ExpenseId).NotEmpty();
}

internal sealed class DeleteExpenseCommandHandler(IAppDbContext db, IExpenseAttachmentStore attachments)
    : IRequestHandler<DeleteExpenseCommand, Result>
{
    public async Task<Result> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await ExpenseRepository.FindAsync(db, request.ExpenseId, cancellationToken);

        if (expense is null)
        {
            return Result.Failure(ExpenseErrors.NotFound(request.ExpenseId));
        }

        if (!expense.IsEditable)
        {
            return Result.Failure(ExpenseErrors.NotEditable);
        }

        // Files first: an orphaned row is recoverable, an orphaned file is invisible clutter that
        // nothing will ever clean up.
        foreach (var attachment in expense.Attachments)
        {
            await attachments.DeleteAsync(attachment.StoredPath, cancellationToken);
        }

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
