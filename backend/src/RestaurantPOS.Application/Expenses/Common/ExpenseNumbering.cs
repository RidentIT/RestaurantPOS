using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Application.Expenses.Common;

/// <summary>Hands out the yearly expense sequence behind "EXP-001-2026" (EXP-009, BR-EXP-003).</summary>
public static class ExpenseNumbering
{
    /// <summary>
    /// The next number for a year. Taken from the highest already issued rather than a count, so
    /// a rejected expense keeps its number and the sequence never hands the same one out twice.
    /// </summary>
    public static async Task<int> NextNumberAsync(IAppDbContext db, int year, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        var highest = await db.Expenses
            .Where(e => e.Year == year)
            .MaxAsync(e => (int?)e.Number, cancellationToken);

        return (highest ?? 0) + 1;
    }
}
