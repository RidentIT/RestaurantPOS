using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.RemoveExpenseAttachment;

/// <summary>Removes a receipt filed against an expense that has not yet been ruled on.</summary>
public sealed record RemoveExpenseAttachmentCommand(Guid ExpenseId, Guid AttachmentId)
    : IRequest<Result<ExpenseDto>>;

public sealed class RemoveExpenseAttachmentCommandValidator : AbstractValidator<RemoveExpenseAttachmentCommand>
{
    public RemoveExpenseAttachmentCommandValidator()
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
        RuleFor(x => x.AttachmentId).NotEmpty();
    }
}

internal sealed class RemoveExpenseAttachmentCommandHandler(IAppDbContext db, IExpenseAttachmentStore store)
    : IRequestHandler<RemoveExpenseAttachmentCommand, Result<ExpenseDto>>
{
    public async Task<Result<ExpenseDto>> Handle(
        RemoveExpenseAttachmentCommand request, CancellationToken cancellationToken)
    {
        var expense = await ExpenseRepository.FindAsync(db, request.ExpenseId, cancellationToken);

        if (expense is null)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.NotFound(request.ExpenseId));
        }

        var attachment = expense.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId);

        if (attachment is null)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.AttachmentNotFound(request.AttachmentId));
        }

        if (!expense.IsEditable)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.NotEditable);
        }

        expense.RemoveAttachment(attachment);
        db.ExpenseAttachments.Remove(attachment);
        await store.DeleteAsync(attachment.StoredPath, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await ExpenseResultFactory.BuildAsync(db, expense, cancellationToken));
    }
}
