using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.CreateExpenseCategory;

/// <summary>Adds a custom expense category (EXP-011), optionally under an existing one.</summary>
public sealed record CreateExpenseCategoryCommand(
    string Name, string? Description, decimal? MonthlyBudget, Guid? ParentCategoryId)
    : IRequest<Result<ExpenseCategoryDto>>;

public sealed class CreateExpenseCategoryCommandValidator : AbstractValidator<CreateExpenseCategoryCommand>
{
    public CreateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ExpenseCategory.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(ExpenseCategory.DescriptionMaxLength);
        RuleFor(x => x.MonthlyBudget).GreaterThanOrEqualTo(0).When(x => x.MonthlyBudget.HasValue);
    }
}

internal sealed class CreateExpenseCategoryCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateExpenseCategoryCommand, Result<ExpenseCategoryDto>>
{
    public async Task<Result<ExpenseCategoryDto>> Handle(
        CreateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        var taken = await db.ExpenseCategories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower(), cancellationToken);

        if (taken)
        {
            return Result.Failure<ExpenseCategoryDto>(ExpenseErrors.CategoryNameTaken);
        }

        ExpenseCategory? parent = null;

        if (request.ParentCategoryId is { } parentId)
        {
            parent = await db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);

            if (parent is null)
            {
                return Result.Failure<ExpenseCategoryDto>(ExpenseErrors.CategoryNotFound(parentId));
            }

            // One level only: a deeper tree makes "total for Utilities" ambiguous about how far
            // down it should reach, and nobody running a restaurant wants to litigate that.
            if (parent.ParentCategoryId is not null)
            {
                return Result.Failure<ExpenseCategoryDto>(ExpenseErrors.CategoryNestingTooDeep);
            }
        }

        var category = ExpenseCategory.Create(
            name, request.Description, request.MonthlyBudget, request.ParentCategoryId);

        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(category.ToDto(parent?.Name));
    }
}
