using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.SetExpenseCategoryActive;

/// <summary>
/// Retires a category or brings it back. Retiring rather than deleting is what keeps last year's
/// reports intact when the restaurant stops using a category.
/// </summary>
public sealed record SetExpenseCategoryActiveCommand(Guid CategoryId, bool IsActive)
    : IRequest<Result<ExpenseCategoryDto>>;

public sealed class SetExpenseCategoryActiveCommandValidator : AbstractValidator<SetExpenseCategoryActiveCommand>
{
    public SetExpenseCategoryActiveCommandValidator() => RuleFor(x => x.CategoryId).NotEmpty();
}

internal sealed class SetExpenseCategoryActiveCommandHandler(IAppDbContext db)
    : IRequestHandler<SetExpenseCategoryActiveCommand, Result<ExpenseCategoryDto>>
{
    public async Task<Result<ExpenseCategoryDto>> Handle(
        SetExpenseCategoryActiveCommand request, CancellationToken cancellationToken)
    {
        var category = await db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<ExpenseCategoryDto>(ExpenseErrors.CategoryNotFound(request.CategoryId));
        }

        if (request.IsActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        var expenseCount = await db.Expenses.CountAsync(e => e.CategoryId == category.Id, cancellationToken);

        return Result.Success(category.ToDto(expenseCount: expenseCount));
    }
}
