using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Expenses.Common;

/// <summary>
/// The one way to load an expense.
/// </summary>
/// <remarks>
/// The approval trail and attachments sit behind private backing fields, so a handler that loads
/// an expense without eager-loading them sees an empty history and quietly writes a transition on
/// top of nothing. Centralised here rather than left to each handler to remember, for the same
/// reason orders are.
/// </remarks>
public static class ExpenseRepository
{
    public static Task<Expense?> FindAsync(IAppDbContext db, Guid expenseId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        return WithAggregate(db.Expenses).FirstOrDefaultAsync(e => e.Id == expenseId, cancellationToken);
    }

    public static IQueryable<Expense> WithAggregate(IQueryable<Expense> expenses)
    {
        ArgumentNullException.ThrowIfNull(expenses);

        return expenses
            .Include(e => e.ApprovalTrail)
            .Include(e => e.Attachments);
    }
}
