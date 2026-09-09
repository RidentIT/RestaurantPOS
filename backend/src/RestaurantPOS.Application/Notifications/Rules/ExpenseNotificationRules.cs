using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Notifications.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Notifications.Rules;

/// <summary>Alerts about spending: approvals waiting, budgets, rejections and unpaid bills.</summary>
internal static class ExpenseNotificationRules
{
    public static async Task<IReadOnlyCollection<NotificationCandidate>> EvaluateAsync(
        IAppDbContext db, DateTime nowUtc, DateOnly today, NotificationThresholds thresholds,
        CancellationToken cancellationToken)
    {
        var candidates = new List<NotificationCandidate>();

        var awaiting = await db.Expenses.AsNoTracking()
            .Where(e => e.Status == ExpenseStatus.Draft || e.Status == ExpenseStatus.Pending)
            .Select(e => new { e.Id, e.Amount })
            .ToListAsync(cancellationToken);

        if (awaiting.Count > 0)
        {
            var total = awaiting.Sum(e => e.Amount);

            // One notification for the whole queue rather than one per expense: a manager wants
            // to know there is a pile to review, not to be told five separate times.
            candidates.Add(new NotificationCandidate(
                NotificationType.ExpensesAwaitingApproval,
                $"ExpensesAwaiting:{awaiting.Count}:{total:0}",
                $"{awaiting.Count} expense{(awaiting.Count == 1 ? "" : "s")} awaiting approval",
                $"Worth {total:0.00} in total.",
                "/expenses"));
        }

        candidates.AddRange(await BudgetsAsync(db, today, thresholds, cancellationToken));
        candidates.AddRange(await RecentlyRejectedAsync(db, nowUtc, cancellationToken));
        candidates.AddRange(await RecurringCreatedAsync(db, nowUtc, cancellationToken));
        candidates.AddRange(await OverdueUnpaidAsync(db, today, cancellationToken));

        return candidates;
    }

    private static async Task<IReadOnlyCollection<NotificationCandidate>> BudgetsAsync(
        IAppDbContext db, DateOnly today, NotificationThresholds thresholds, CancellationToken cancellationToken)
    {
        var from = new DateOnly(today.Year, today.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var expenses = await ExpenseAnalytics.ApprovedExpensesBetweenAsync(db, from, to, cancellationToken);
        var categories = await db.ExpenseCategories.AsNoTracking()
            .Where(c => c.MonthlyBudget != null && c.MonthlyBudget > 0)
            .ToListAsync(cancellationToken);

        var spent = expenses
            .GroupBy(e => e.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var nearAt = thresholds.For(NotificationType.ExpenseCategoryNearingBudget);
        var candidates = new List<NotificationCandidate>();
        var month = from.ToString("yyyy-MM");

        foreach (var category in categories)
        {
            var budget = category.MonthlyBudget!.Value;
            var used = spent.GetValueOrDefault(category.Id);
            var percentage = used / budget * 100m;

            if (used > budget)
            {
                candidates.Add(new NotificationCandidate(
                    NotificationType.ExpenseCategoryOverBudget,
                    $"OverBudget:{category.Id}:{month}",
                    $"{category.Name} is over budget",
                    $"{used:0.00} spent against a budget of {budget:0.00} — over by {used - budget:0.00}.",
                    "/expenses/reports"));
            }
            else if (nearAt > 0 && percentage >= nearAt)
            {
                candidates.Add(new NotificationCandidate(
                    NotificationType.ExpenseCategoryNearingBudget,
                    $"NearBudget:{category.Id}:{month}",
                    $"{category.Name} is at {percentage:0}% of budget",
                    $"{budget - used:0.00} left of {budget:0.00} this month.",
                    "/expenses/reports"));
            }
        }

        return candidates;
    }

    private static async Task<IReadOnlyCollection<NotificationCandidate>> RecentlyRejectedAsync(
        IAppDbContext db, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var since = nowUtc.AddDays(-7);

        var rejected = await db.Expenses.AsNoTracking()
            .Where(e => e.Status == ExpenseStatus.Rejected && e.ApprovedAtUtc >= since)
            .Select(e => new { e.Id, e.Number, e.Year, e.Amount, e.ApprovalComments, e.RecordedByUserId })
            .ToListAsync(cancellationToken);

        // Addressed to whoever recorded it, which the dispatcher honours through the target user.
        return [.. rejected.Select(e => new NotificationCandidate(
            NotificationType.ExpenseRejected,
            $"ExpenseRejected:{e.Id}",
            $"EXP-{e.Number:000}-{e.Year} was rejected",
            e.ApprovalComments is null
                ? $"Your expense for {e.Amount:0.00} was turned down."
                : $"Your expense for {e.Amount:0.00} was turned down: “{e.ApprovalComments}”",
            "/expenses"))];
    }

    private static async Task<IReadOnlyCollection<NotificationCandidate>> RecurringCreatedAsync(
        IAppDbContext db, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var since = nowUtc.AddHours(-24);

        var created = await db.Expenses.AsNoTracking()
            .Where(e => e.RecurringExpenseId != null
                && e.Status == ExpenseStatus.Draft
                && e.CreatedAtUtc >= since)
            .Select(e => new { e.Id, e.Amount })
            .ToListAsync(cancellationToken);

        if (created.Count == 0)
        {
            return [];
        }

        return
        [
            new NotificationCandidate(
                NotificationType.RecurringExpensesCreated,
                $"RecurringCreated:{nowUtc:yyyy-MM}",
                $"{created.Count} recurring expense{(created.Count == 1 ? "" : "s")} created",
                $"Worth {created.Sum(e => e.Amount):0.00}. They are drafts until you approve them.",
                "/expenses"),
        ];
    }

    private static async Task<IReadOnlyCollection<NotificationCandidate>> OverdueUnpaidAsync(
        IAppDbContext db, DateOnly today, CancellationToken cancellationToken)
    {
        var overdue = await db.Expenses.AsNoTracking()
            .Where(e => e.Status == ExpenseStatus.Approved
                && !e.IsPaid
                && e.PaymentDate != null
                && e.PaymentDate < today)
            .Select(e => new { e.Id, e.Number, e.Year, e.Amount, e.PaymentDate })
            .ToListAsync(cancellationToken);

        return [.. overdue.Select(e => new NotificationCandidate(
            NotificationType.ExpenseOverdueUnpaid,
            $"ExpenseUnpaid:{e.Id}",
            $"EXP-{e.Number:000}-{e.Year} is still unpaid",
            $"{e.Amount:0.00} was due on {e.PaymentDate:yyyy-MM-dd}.",
            "/expenses"))];
    }
}
