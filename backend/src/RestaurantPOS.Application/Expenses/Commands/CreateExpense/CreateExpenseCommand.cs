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

namespace RestaurantPOS.Application.Expenses.Commands.CreateExpense;

/// <summary>
/// Records money paid out (EXP-001 to EXP-008). Created as a draft; whoever records it can put it
/// forward, and a manager can approve it there and then.
/// </summary>
/// <param name="SubmitForApproval">
/// Sends the expense straight to Pending instead of leaving it a draft — the "Submit for
/// Approval" button rather than "Save as Draft".
/// </param>
public sealed record CreateExpenseCommand(
    DateOnly ExpenseDate,
    Guid CategoryId,
    decimal Amount,
    string? Description,
    ExpensePaymentMethod PaymentMethod,
    string? PaymentReference,
    DateOnly? PaymentDate,
    bool IsPaid,
    bool SubmitForApproval) : IRequest<Result<ExpenseDto>>;

public sealed class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
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

internal sealed class CreateExpenseCommandHandler(
    IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<CreateExpenseCommand, Result<ExpenseDto>>
{
    public async Task<Result<ExpenseDto>> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        // Backdating is expected — a manager keys in yesterday's bills this morning (BR-EXP-017).
        // A future date is not: it would book spending into a day that has not happened.
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

        var number = await ExpenseNumbering.NextNumberAsync(db, request.ExpenseDate.Year, cancellationToken);
        var now = clock.UtcNow;

        var expense = Expense.Create(
            number,
            request.ExpenseDate,
            request.CategoryId,
            request.Amount,
            request.Description,
            request.PaymentMethod,
            request.PaymentReference,
            request.PaymentDate,
            request.IsPaid,
            currentUser.UserId!.Value);

        if (request.SubmitForApproval)
        {
            expense.Submit(currentUser.UserId!.Value, now);
        }

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await ExpenseRepository.FindAsync(db, expense.Id, cancellationToken);

        return Result.Success(await ExpenseResultFactory.BuildAsync(db, saved!, cancellationToken));
    }
}
