using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.UpdateExpenseCategory;

/// <summary>Renames a category or changes its budget. Built-in categories can be edited, not deleted.</summary>
public sealed record UpdateExpenseCategoryCommand(
    Guid CategoryId, string Name, string? Description, decimal? MonthlyBudget, Guid? ParentCategoryId)
    : IRequest<Result<ExpenseCategoryDto>>;

public sealed class UpdateExpenseCategoryCommandValidator : AbstractValidator<UpdateExpenseCategoryCommand>
{
    public UpdateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ExpenseCategory.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(ExpenseCategory.DescriptionMaxLength);
        RuleFor(x => x.MonthlyBudget).GreaterThanOrEqualTo(0).When(x => x.MonthlyBudget.HasValue);
    }
}

internal sealed class UpdateExpenseCategoryCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateExpenseCategoryCommand, Result<ExpenseCategoryDto>>
{
    public async Task<Result<ExpenseCategoryDto>> Handle(
        UpdateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<ExpenseCategoryDto>(ExpenseErrors.CategoryNotFound(request.CategoryId));
        }

        var name = request.Name.Trim();

        var taken = await db.ExpenseCategories
            .AnyAsync(c => c.Id != request.CategoryId && c.Name.ToLower() == name.ToLower(), cancellationToken);

        if (taken)
        {
            return Result.Failure<ExpenseCategoryDto>(ExpenseErrors.CategoryNameTaken);
        }

        ExpenseCategory? parent = null;

        if (request.ParentCategoryId is { } parentId)
        {
            if (parentId == request.CategoryId)
            {
                return Result.Failure<ExpenseCategoryDto>(ExpenseErrors.CategoryOwnParent);
            }

            parent = await db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);

            if (parent is null)
            {
                return Result.Failure<ExpenseCategoryDto>(ExpenseErrors.CategoryNotFound(parentId));
            }

            if (parent.ParentCategoryId is not null)
            {
                return Result.Failure<ExpenseCategoryDto>(ExpenseErrors.CategoryNestingTooDeep);
            }

            // Taking on a parent while having children of its own would build the second level
            // this model deliberately does not have.
            var hasChildren = await db.ExpenseCategories
                .AnyAsync(c => c.ParentCategoryId == request.CategoryId, cancellationToken);

            if (hasChildren)
            {
                return Result.Failure<ExpenseCategoryDto>(ExpenseErrors.CategoryNestingTooDeep);
            }
        }

        category.UpdateDetails(name, request.Description, request.MonthlyBudget, request.ParentCategoryId);
        await db.SaveChangesAsync(cancellationToken);

        var expenseCount = await db.Expenses.CountAsync(e => e.CategoryId == category.Id, cancellationToken);

        return Result.Success(category.ToDto(parent?.Name, expenseCount));
    }
}
