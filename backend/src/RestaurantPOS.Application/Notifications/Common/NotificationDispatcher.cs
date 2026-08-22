using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Notifications;

namespace RestaurantPOS.Application.Notifications.Common;

/// <summary>
/// Decides who hears about each candidate, and whether it has been said recently enough to keep
/// quiet.
/// </summary>
/// <remarks>
/// Three gates, in order: is this person allowed to see it (module permissions), have they
/// switched it off (preferences), and has it already been said (cooldown). Keeping them here
/// rather than in the rules means a new rule inherits all three for free.
/// </remarks>
public static class NotificationDispatcher
{
    /// <summary>
    /// How long a repeat of the same thing stays quiet. A low stock level is still true a minute
    /// later, and re-announcing it every evaluation would make the bell worthless — but a
    /// fortnight of silence about an unreordered ingredient would be just as bad, so it resurfaces
    /// once a day.
    /// </summary>
    private static readonly TimeSpan Cooldown = TimeSpan.FromHours(24);

    /// <summary>
    /// Turns candidates into notifications for whoever should receive them, and returns how many
    /// were actually raised.
    /// </summary>
    /// <param name="targetedUserIds">
    /// Restricts a candidate to specific people, keyed by dedupe key. Used where a notification is
    /// personal — an expense rejection belongs to whoever recorded it, not to every manager.
    /// </param>
    public static async Task<int> DispatchAsync(
        IAppDbContext db,
        IReadOnlyCollection<NotificationCandidate> candidates,
        DateTime nowUtc,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, Guid>? targetedUserIds = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            return 0;
        }

        var recipients = await LoadRecipientsAsync(db, cancellationToken);

        if (recipients.Count == 0)
        {
            return 0;
        }

        var preferences = await db.NotificationPreferences.AsNoTracking()
            .ToListAsync(cancellationToken);

        var preferenceLookup = preferences.ToDictionary(p => (p.UserId, p.Type), p => p.IsEnabled);

        var keys = candidates.Select(c => c.DedupeKey).Distinct().ToList();
        var cooldownStart = nowUtc - Cooldown;

        var alreadyRaised = await db.Notifications.AsNoTracking()
            .Where(n => keys.Contains(n.DedupeKey) && n.RaisedAtUtc >= cooldownStart)
            .Select(n => new { n.UserId, n.DedupeKey })
            .ToListAsync(cancellationToken);

        var seen = alreadyRaised
            .Select(n => (n.UserId, n.DedupeKey))
            .ToHashSet();

        var raised = 0;

        foreach (var candidate in candidates)
        {
            if (!NotificationCatalog.IsDefined(candidate.Type))
            {
                continue;
            }

            var descriptor = NotificationCatalog.Describe(candidate.Type);

            var audience = targetedUserIds is not null
                && targetedUserIds.TryGetValue(candidate.DedupeKey, out var only)
                    ? recipients.Where(r => r.UserId == only)
                    : recipients.Where(r => IsEligible(r, descriptor));

            foreach (var recipient in audience)
            {
                var enabled = preferenceLookup.TryGetValue((recipient.UserId, candidate.Type), out var chosen)
                    ? chosen
                    : descriptor.DefaultEnabled;

                if (!enabled || !seen.Add((recipient.UserId, candidate.DedupeKey)))
                {
                    continue;
                }

                db.Notifications.Add(Notification.Raise(
                    recipient.UserId,
                    candidate.Type,
                    descriptor.Severity,
                    candidate.Title,
                    candidate.Body,
                    candidate.DedupeKey,
                    candidate.Link,
                    nowUtc));

                raised++;
            }
        }

        if (raised > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return raised;
    }

    /// <summary>Administrators hold every module implicitly, so their grants are not consulted.</summary>
    private static bool IsEligible(Recipient recipient, NotificationDescriptor descriptor) =>
        recipient.IsAdmin || descriptor.Modules.Any(recipient.Modules.Contains);

    private static async Task<IReadOnlyCollection<Recipient>> LoadRecipientsAsync(
        IAppDbContext db, CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .Select(u => new { u.Id, u.Role })
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();

        var grants = await db.UserModulePermissions.AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.Module })
            .ToListAsync(cancellationToken);

        var byUser = grants
            .GroupBy(g => g.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Module).ToHashSet());

        return [.. users.Select(u => new Recipient(
            u.Id,
            u.Role == UserRole.Admin,
            byUser.GetValueOrDefault(u.Id, [])))];
    }

    private sealed record Recipient(Guid UserId, bool IsAdmin, HashSet<AppModule> Modules);
}
