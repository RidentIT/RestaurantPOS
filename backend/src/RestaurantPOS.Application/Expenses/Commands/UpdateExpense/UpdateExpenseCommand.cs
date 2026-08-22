using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.UpdateExpense;

/// <summary>
/// Corrects an expense before anyone has ruled on it (EXP-035). Once approved it is frozen
/// (EXP-036) — it has already been counted into a day's profit, so the way to fix an approved
/// expense is to reject it and record the right one, leaving both on the books.
/// </summary>
public sealed record UpdateExpenseCommand(
    Guid ExpenseId,
    DateOnly ExpenseDate,
    Guid CategoryId,
    decimal Amount,
    string? Description,
    ExpensePaymentMethod PaymentMethod,
    string? PaymentReference,
    DateOnly? PaymentDate,
    bool IsPaid) : IRequest<Result<ExpenseDto>>;

public sealed class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator()
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("The amount must be greater than zero.");
        RuleFor(x => x.Description).MaximumLength(Expense.DescriptionMaxLength);
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.PaymentReference).MaximumLength(Expense.ReferenceMaxLength);

        RuleFor(x => x.PaymentReference)
            .NotEmpty()
            .When(x => x.PaymentMethod != ExpensePaymentMethod.Cash)
            .WithMessage("A reference is required for cheque, card and bank transfer payments.");
    }
}

internal sealed class UpdateExpenseCommandHandler(IAppDbContext db, IDateTimeProvider clock)
    : IRequestHandler<UpdateExpenseCommand, Result<ExpenseDto>>
{
    public async Task<Result<ExpenseDto>> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await ExpenseRepository.FindAsync(db, request.ExpenseId, cancellationToken);

        if (expense is null)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.NotFound(request.ExpenseId));
        }

        if (!expense.IsEditable)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.NotEditable);
        }

        if (request.ExpenseDate > clock.Today)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.FutureDate);
        }

        var category = await db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.CategoryNotFound(request.CategoryId));
        }

        if (!category.IsActive)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.CategoryInactive);
        }

        expense.UpdateDetails(
            request.ExpenseDate,
            request.CategoryId,
            request.Amount,
            request.Description,
            request.PaymentMethod,
            request.PaymentReference,
            request.PaymentDate,
            request.IsPaid);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await ExpenseResultFactory.BuildAsync(db, expense, cancellationToken));
    }
}
