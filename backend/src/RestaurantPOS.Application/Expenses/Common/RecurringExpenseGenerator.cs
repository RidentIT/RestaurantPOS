using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Expenses.Common;

/// <summary>
/// Materialises standing monthly costs into real expenses (BR-EXP-008).
/// </summary>
/// <remarks>
/// Run whenever the expense screens are opened rather than on a timer. The restaurant's machine
/// is switched off overnight and may not be turned on for days, so anything that fires "on the
/// 1st" would simply miss a month; asking on every visit instead means the rent appears the first
/// time somebody looks, whenever that is.
/// <para>
/// Safe to call repeatedly: each instruction records the month it last produced, so opening the
/// screen five times in a morning still yields one rent. Everything is generated as a draft —
/// never approved — so a figure that has changed is corrected before it reaches any report.
/// </para>
/// </remarks>
public static class RecurringExpenseGenerator
{
    /// <summary>
    /// Creates any recurring expense owed for the month containing <paramref name="today"/>, plus
    /// any month missed since each instruction last ran, and returns how many were created.
    /// </summary>
    public static async Task<int> GenerateDueAsync(
        IAppDbContext db, DateOnly today, Guid actingUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        var instructions = await db.RecurringExpenses
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        if (instructions.Count == 0)
        {
            return 0;
        }

        var categoryIds = instructions.Select(r => r.CategoryId).Distinct().ToList();

        var activeCategories = await db.ExpenseCategories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id) && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var created = 0;

        // The yearly sequence is tracked per year in memory across the whole run: several months
        // may be generated at once, and nothing is written to the database until the end, so
        // re-querying the highest number would hand the same one out twice.
        var nextNumberByYear = new Dictionary<int, int>();

        foreach (var instruction in instructions)
        {
            // A retired category cannot take new expenses, so its standing instruction waits
            // rather than failing the whole generation run.
            if (!activeCategories.Contains(instruction.CategoryId))
            {
                continue;
            }

            foreach (var (year, month) in MonthsDue(instruction, today))
            {
                // Belt and braces against a half-finished earlier run: the marker says it is due,
                // but an expense for that month already exists.
                var alreadyExists = await db.Expenses.AnyAsync(
                    e => e.RecurringExpenseId == instruction.Id
                        && e.ExpenseDate.Year == year
                        && e.ExpenseDate.Month == month,
                    cancellationToken);

                if (!alreadyExists)
                {
                    if (!nextNumberByYear.TryGetValue(year, out var number))
                    {
                        number = await ExpenseNumbering.NextNumberAsync(db, year, cancellationToken);
                    }

                    nextNumberByYear[year] = number + 1;

                    db.Expenses.Add(Expense.Create(
                        number,
                        instruction.DateFor(year, month),
                        instruction.CategoryId,
                        instruction.Amount,
                        instruction.Description,
                        instruction.PaymentMethod,
                        // A standing instruction has no cheque number of its own; the manager
                        // fills one in when the payment is actually made.
                        instruction.PaymentMethod == ExpensePaymentMethod.Cash ? null : "Pending",
                        paymentDate: null,
                        isPaid: false,
                        actingUserId,
                        instruction.Id));

                    created++;
                }

                instruction.MarkGenerated(year, month);
            }
        }

        // Saved unconditionally: even when nothing new was created, the "last generated" markers
        // may have moved forward past months that turned out to already exist.
        await db.SaveChangesAsync(cancellationToken);

        return created;
    }

    /// <summary>
    /// Every month an instruction still owes, oldest first. A machine unopened since March
    /// produces March, April and May rather than only the current month.
    /// </summary>
    private static IEnumerable<(int Year, int Month)> MonthsDue(RecurringExpense instruction, DateOnly today)
    {
        var cursor = instruction.LastGeneratedYear is { } lastYear && instruction.LastGeneratedMonth is { } lastMonth
            ? new DateOnly(lastYear, lastMonth, 1).AddMonths(1)
            // Never run before: start from the month it was set up in, not from whenever the
            // restaurant opened, so adding rent today does not back-fill the whole year.
            : new DateOnly(
                DateOnly.FromDateTime(instruction.CreatedAtUtc).Year,
                DateOnly.FromDateTime(instruction.CreatedAtUtc).Month,
                1);

        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        while (cursor <= currentMonth)
        {
            yield return (cursor.Year, cursor.Month);
            cursor = cursor.AddMonths(1);
        }
    }
}
