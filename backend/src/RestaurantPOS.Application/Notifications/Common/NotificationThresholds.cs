using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Notifications;

namespace RestaurantPOS.Application.Notifications.Common;

/// <summary>
/// The tunable numbers behind the rules, loaded once per evaluation.
/// </summary>
/// <remarks>
/// Falls back to the catalog's default when an administrator has never set one, so a fresh
/// install behaves sensibly with an empty settings table and changing a default in code reaches
/// every restaurant that never overrode it.
/// </remarks>
public sealed class NotificationThresholds
{
    private readonly Dictionary<NotificationType, decimal> _configured;

    private NotificationThresholds(Dictionary<NotificationType, decimal> configured) =>
        _configured = configured;

    public static async Task<NotificationThresholds> LoadAsync(
        IAppDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        var configured = await db.NotificationSettings.AsNoTracking()
            .ToDictionaryAsync(s => s.Type, s => s.Threshold, cancellationToken);

        return new NotificationThresholds(configured);
    }

    /// <summary>The configured value, or the catalog default when nobody has set one.</summary>
    public decimal For(NotificationType type) =>
        _configured.TryGetValue(type, out var value)
            ? value
            : NotificationCatalog.Describe(type).DefaultThreshold ?? 0m;

    /// <summary>The configured value as whole minutes or days, for rules that count in them.</summary>
    public int WholeFor(NotificationType type) => (int)Math.Round(For(type), MidpointRounding.AwayFromZero);

    /// <summary>
    /// The configured value as a double, for comparing against <see cref="TimeSpan"/> totals,
    /// which are doubles.
    /// </summary>
    public double ElapsedFor(NotificationType type) => (double)For(type);
}
