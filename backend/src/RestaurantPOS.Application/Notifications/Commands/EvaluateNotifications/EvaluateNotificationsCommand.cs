using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Notifications.Common;
using RestaurantPOS.Application.Notifications.Rules;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Notifications.Commands.EvaluateNotifications;

/// <summary>
/// Works out what is worth telling people about right now, and raises it.
/// </summary>
/// <remarks>
/// Called by the bell as it polls, rather than by a background timer. The restaurant's machine is
/// switched off overnight, so a scheduler would either miss its window or fire into an empty
/// room; evaluating when somebody is actually looking means alerts are there when opened and no
/// work happens when nobody is.
/// <para>
/// Every rule reads existing data rather than being raised inline by the commands that cause
/// them. That keeps the whole module additive — no existing use case had to be touched to make
/// notifications work, so none of them can be broken by it either.
/// </para>
/// </remarks>
public sealed record EvaluateNotificationsCommand : IRequest<Result<int>>;

internal sealed class EvaluateNotificationsCommandHandler(IAppDbContext db, IDateTimeProvider clock)
    : IRequestHandler<EvaluateNotificationsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        EvaluateNotificationsCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var today = clock.Today;
        var thresholds = await NotificationThresholds.LoadAsync(db, cancellationToken);

        var candidates = new List<NotificationCandidate>();

        candidates.AddRange(await StockNotificationRules.EvaluateAsync(db, now, thresholds, cancellationToken));
        candidates.AddRange(await ServiceNotificationRules.EvaluateAsync(db, now, thresholds, cancellationToken));
        candidates.AddRange(await SupplierNotificationRules.EvaluateAsync(db, now, today, thresholds, cancellationToken));
        candidates.AddRange(await ExpenseNotificationRules.EvaluateAsync(db, now, today, thresholds, cancellationToken));
        candidates.AddRange(await AdministrationNotificationRules.EvaluateAsync(db, now, cancellationToken));

        // A rejected expense belongs to whoever recorded it, not to everyone who can see expenses.
        var targeted = await ResolveRejectedExpenseRecipientsAsync(db, candidates, cancellationToken);

        var raised = await NotificationDispatcher.DispatchAsync(
            db, candidates, now, cancellationToken, targeted);

        await PurgeOldNotificationsAsync(now, cancellationToken);

        return Result.Success(raised);
    }

    private static async Task<IReadOnlyDictionary<string, Guid>> ResolveRejectedExpenseRecipientsAsync(
        IAppDbContext db,
        IReadOnlyCollection<NotificationCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var expenseIds = candidates
            .Where(c => c.Type == NotificationType.ExpenseRejected)
            .Select(c => c.DedupeKey.Split(':').Last())
            .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (expenseIds.Count == 0)
        {
            return new Dictionary<string, Guid>();
        }

        var recorders = await db.Expenses.AsNoTracking()
            .Where(e => expenseIds.Contains(e.Id))
            .Select(e => new { e.Id, e.RecordedByUserId })
            .ToListAsync(cancellationToken);

        return recorders.ToDictionary(r => $"ExpenseRejected:{r.Id}", r => r.RecordedByUserId);
    }

    /// <summary>
    /// Drops read notifications after a month so the table cannot grow without bound on a machine
    /// nobody ever administers. Unread ones are kept however old — something nobody has looked at
    /// is not something to quietly throw away.
    /// </summary>
    private async Task PurgeOldNotificationsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var cutoff = nowUtc.AddDays(-30);

        var stale = await db.Notifications
            .Where(n => n.ReadAtUtc != null && n.RaisedAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return;
        }

        db.Notifications.RemoveRange(stale);
        await db.SaveChangesAsync(cancellationToken);
    }
}
