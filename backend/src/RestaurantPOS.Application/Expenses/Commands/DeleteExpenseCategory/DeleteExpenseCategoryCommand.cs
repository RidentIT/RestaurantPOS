using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.DeleteExpenseCategory;

/// <summary>
/// Removes a custom category that has never been used. Anything with history behind it — or any
/// built-in category — is refused, because deleting it would leave recorded spending pointing at
/// a category that no longer exists.
/// </summary>
public sealed record DeleteExpenseCategoryCommand(Guid CategoryId) : IRequest<Result>;

public sealed class DeleteExpenseCategoryCommandValidator : AbstractValidator<DeleteExpenseCategoryCommand>
{
    public DeleteExpenseCategoryCommandValidator() => RuleFor(x => x.CategoryId).NotEmpty();
}

internal sealed class DeleteExpenseCategoryCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteExpenseCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure(ExpenseErrors.CategoryNotFound(request.CategoryId));
        }

        if (category.IsSystem)
        {
            return Result.Failure(ExpenseErrors.CategoryIsSystem);
        }

        var inUse = await db.Expenses.AnyAsync(e => e.CategoryId == category.Id, cancellationToken)
            || await db.RecurringExpenses.AnyAsync(r => r.CategoryId == category.Id, cancellationToken)
            || await db.ExpenseCategories.AnyAsync(c => c.ParentCategoryId == category.Id, cancellationToken);

        if (inUse)
        {
            return Result.Failure(ExpenseErrors.CategoryInUse);
        }

        db.ExpenseCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
