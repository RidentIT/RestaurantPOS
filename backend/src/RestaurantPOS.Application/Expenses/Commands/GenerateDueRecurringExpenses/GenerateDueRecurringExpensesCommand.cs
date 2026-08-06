using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Expenses.Commands.GenerateDueRecurringExpenses;

/// <summary>
/// Creates any recurring expense the restaurant still owes for this month, and for any month
/// missed since (BR-EXP-008).
/// </summary>
/// <remarks>
/// Called by the expenses screen on load, which is what makes recurring costs appear without a
/// scheduler. Safe to call as often as the screen is opened — each instruction remembers the last
/// month it produced.
/// </remarks>
public sealed record GenerateDueRecurringExpensesCommand : IRequest<Result<int>>;

internal sealed class GenerateDueRecurringExpensesCommandHandler(
    IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<GenerateDueRecurringExpensesCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        GenerateDueRecurringExpensesCommand request, CancellationToken cancellationToken)
    {
        var created = await RecurringExpenseGenerator.GenerateDueAsync(
            db, clock.Today, currentUser.UserId!.Value, cancellationToken);

        return Result.Success(created);
    }
}
