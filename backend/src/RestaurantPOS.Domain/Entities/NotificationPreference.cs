using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One person's choice about one kind of notification.
/// </summary>
/// <remarks>
/// Rows only exist where somebody has changed something. No row means "whatever the catalog says
/// by default", which keeps a fresh install empty and means changing a default in code takes
/// effect for everyone who never expressed an opinion.
/// </remarks>
public sealed class NotificationPreference : BaseEntity
{
    // EF Core materialisation.
    private NotificationPreference()
    {
    }

    private NotificationPreference(Guid userId, NotificationType type, bool isEnabled, bool desktopEnabled)
    {
        UserId = userId;
        Type = type;
        IsEnabled = isEnabled;
        DesktopEnabled = desktopEnabled;
    }

    public Guid UserId { get; private set; }

    public NotificationType Type { get; private set; }

    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Whether this also raises a Windows notification, which shows even when the POS is behind
    /// another window. Wanted on an unattended kitchen screen; a nuisance at a busy till.
    /// </summary>
    public bool DesktopEnabled { get; private set; }

    public static NotificationPreference Create(
        Guid userId, NotificationType type, bool isEnabled, bool desktopEnabled = false) =>
        new(userId, type, isEnabled, desktopEnabled);

    public void Update(bool isEnabled, bool desktopEnabled)
    {
        IsEnabled = isEnabled;
        DesktopEnabled = desktopEnabled;
    }
}
