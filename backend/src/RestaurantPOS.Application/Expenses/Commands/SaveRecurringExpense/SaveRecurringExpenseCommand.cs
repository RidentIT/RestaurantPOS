using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.SaveRecurringExpense;

/// <summary>
/// Creates or updates a standing monthly cost (EXP-012). One command for both, since the form is
/// identical and the only difference is whether an id came with it.
/// </summary>
public sealed record SaveRecurringExpenseCommand(
    Guid? RecurringExpenseId,
    Guid CategoryId,
    decimal Amount,
    string? Description,
    ExpensePaymentMethod PaymentMethod,
    int DayOfMonth) : IRequest<Result<RecurringExpenseDto>>;

public sealed class SaveRecurringExpenseCommandValidator : AbstractValidator<SaveRecurringExpenseCommand>
{
    public SaveRecurringExpenseCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("The amount must be greater than zero.");
        RuleFor(x => x.Description).MaximumLength(RecurringExpense.DescriptionMaxLength);
        RuleFor(x => x.PaymentMethod).IsInEnum();

        RuleFor(x => x.DayOfMonth)
            .InclusiveBetween(1, 31)
            .WithMessage("The day of the month must be between 1 and 31.");
    }
}

internal sealed class SaveRecurringExpenseCommandHandler(IAppDbContext db)
    : IRequestHandler<SaveRecurringExpenseCommand, Result<RecurringExpenseDto>>
{
    public async Task<Result<RecurringExpenseDto>> Handle(
        SaveRecurringExpenseCommand request, CancellationToken cancellationToken)
    {
        var category = await db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<RecurringExpenseDto>(ExpenseErrors.CategoryNotFound(request.CategoryId));
        }

        if (!category.IsActive)
        {
            return Result.Failure<RecurringExpenseDto>(ExpenseErrors.CategoryInactive);
        }

        RecurringExpense recurring;

        if (request.RecurringExpenseId is { } id)
        {
            var existing = await db.RecurringExpenses.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (existing is null)
            {
                return Result.Failure<RecurringExpenseDto>(ExpenseErrors.RecurringNotFound(id));
            }

            existing.UpdateDetails(
                request.CategoryId, request.Amount, request.Description, request.PaymentMethod, request.DayOfMonth);

            recurring = existing;
        }
        else
        {
            recurring = RecurringExpense.Create(
                request.CategoryId, request.Amount, request.Description, request.PaymentMethod, request.DayOfMonth);

            db.RecurringExpenses.Add(recurring);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(recurring.ToDto(category.Name));
    }
}
