using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Notifications.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Notifications.Rules;

/// <summary>Quieter alerts: account changes and data that will misbehave later if left alone.</summary>
internal static class AdministrationNotificationRules
{
    public static async Task<IReadOnlyCollection<NotificationCandidate>> EvaluateAsync(
        IAppDbContext db, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var candidates = new List<NotificationCandidate>();
        var since = nowUtc.AddHours(-24);

        var changed = await db.Users.AsNoTracking()
            .Where(u => u.CreatedAtUtc >= since || (u.UpdatedAtUtc != null && u.UpdatedAtUtc >= since))
            .Select(u => new { u.Id, u.FullName, u.Username, u.IsActive, u.CreatedAtUtc, u.UpdatedAtUtc })
            .ToListAsync(cancellationToken);

        foreach (var user in changed)
        {
            var isNew = user.CreatedAtUtc >= since;
            var stamp = (user.UpdatedAtUtc ?? user.CreatedAtUtc).Ticks;

            candidates.Add(new NotificationCandidate(
                NotificationType.UserAccountChanged,
                $"UserChanged:{user.Id}:{stamp}",
                isNew ? $"Account created for {user.FullName}" : $"Account changed: {user.FullName}",
                $"@{user.Username} is currently {(user.IsActive ? "active" : "deactivated")}.",
                "/users"));
        }

        // A menu item that sells but deducts nothing quietly drifts the kitchen's stock away from
        // reality, and nothing else in the system will ever complain about it.
        var withoutRecipe = await db.MenuItems.AsNoTracking()
            .Where(m => m.IsActive && !db.Recipes.Any(r => r.MenuItemId == m.Id && r.IsEnabled))
            .Select(m => new { m.Id, m.Name })
            .ToListAsync(cancellationToken);

        candidates.AddRange(withoutRecipe.Select(m => new NotificationCandidate(
            NotificationType.MenuItemWithoutRecipe,
            $"NoRecipe:{m.Id}",
            $"{m.Name} has no recipe",
            "Selling it will not deduct any kitchen stock.",
            "/recipes")));

        return candidates;
    }
}
