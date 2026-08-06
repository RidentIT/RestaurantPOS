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

namespace RestaurantPOS.Application.Expenses.Commands.DecideExpenses;

/// <summary>
/// Approves or rejects expenses (EXP-013, EXP-014).
/// </summary>
/// <remarks>
/// Takes a list rather than a single id because the day's expenses are reviewed together — the
/// manager looks down five bills and signs them off in one go. One expense is just a list of one,
/// so there is no second code path to keep in step.
/// <para>
/// All or nothing: if any expense in the batch has already been decided, none are changed. A
/// partial bulk approval would leave the manager guessing which of the five went through.
/// </para>
/// </remarks>
public sealed record DecideExpensesCommand(
    IReadOnlyCollection<Guid> ExpenseIds, bool Approve, string? Comments)
    : IRequest<Result<IReadOnlyCollection<ExpenseDto>>>;

public sealed class DecideExpensesCommandValidator : AbstractValidator<DecideExpensesCommand>
{
    public DecideExpensesCommandValidator()
    {
        RuleFor(x => x.ExpenseIds).NotEmpty().WithMessage("Choose at least one expense.");
        RuleFor(x => x.Comments).MaximumLength(ExpenseApprovalEntry.CommentsMaxLength);
    }
}

internal sealed class DecideExpensesCommandHandler(
    IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<DecideExpensesCommand, Result<IReadOnlyCollection<ExpenseDto>>>
{
    public async Task<Result<IReadOnlyCollection<ExpenseDto>>> Handle(
        DecideExpensesCommand request, CancellationToken cancellationToken)
    {
        var ids = request.ExpenseIds.Distinct().ToList();

        var expenses = await ExpenseRepository.WithAggregate(db.Expenses)
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var missing = ids.FirstOrDefault(id => expenses.All(e => e.Id != id));
        if (missing != Guid.Empty)
        {
            return Result.Failure<IReadOnlyCollection<ExpenseDto>>(ExpenseErrors.NotFound(missing));
        }

        if (expenses.Any(e => e.Status is ExpenseStatus.Approved or ExpenseStatus.Rejected))
        {
            return Result.Failure<IReadOnlyCollection<ExpenseDto>>(ExpenseErrors.AlreadyDecided);
        }

        var userId = currentUser.UserId!.Value;
        var now = clock.UtcNow;

        foreach (var expense in expenses)
        {
            if (request.Approve)
            {
                expense.Approve(userId, now, request.Comments);
            }
            else
            {
                expense.Reject(userId, now, request.Comments);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var dtos = new List<ExpenseDto>(expenses.Count);

        foreach (var expense in expenses)
        {
            dtos.Add(await ExpenseResultFactory.BuildAsync(db, expense, cancellationToken));
        }

        return Result.Success<IReadOnlyCollection<ExpenseDto>>(dtos);
    }
}
