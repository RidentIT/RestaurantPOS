using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.SetRecurringExpenseActive;

/// <summary>Stops or resumes a standing monthly cost without losing its history.</summary>
public sealed record SetRecurringExpenseActiveCommand(Guid RecurringExpenseId, bool IsActive)
    : IRequest<Result<RecurringExpenseDto>>;

public sealed class SetRecurringExpenseActiveCommandValidator
    : AbstractValidator<SetRecurringExpenseActiveCommand>
{
    public SetRecurringExpenseActiveCommandValidator() => RuleFor(x => x.RecurringExpenseId).NotEmpty();
}

internal sealed class SetRecurringExpenseActiveCommandHandler(IAppDbContext db)
    : IRequestHandler<SetRecurringExpenseActiveCommand, Result<RecurringExpenseDto>>
{
    public async Task<Result<RecurringExpenseDto>> Handle(
        SetRecurringExpenseActiveCommand request, CancellationToken cancellationToken)
    {
        var recurring = await db.RecurringExpenses
            .FirstOrDefaultAsync(r => r.Id == request.RecurringExpenseId, cancellationToken);

        if (recurring is null)
        {
            return Result.Failure<RecurringExpenseDto>(
                ExpenseErrors.RecurringNotFound(request.RecurringExpenseId));
        }

        if (request.IsActive)
        {
            recurring.Activate();
        }
        else
        {
            recurring.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        var categoryName = await db.ExpenseCategories
            .Where(c => c.Id == recurring.CategoryId)
            .Select(c => c.Name)
            .FirstAsync(cancellationToken);

        return Result.Success(recurring.ToDto(categoryName));
    }
}
