using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Notifications;

/// <summary>What a threshold means, so the settings screen can label its input.</summary>
public enum ThresholdUnit
{
    None = 0,
    Minutes = 1,
    Days = 2,
    Percent = 3,
    Amount = 4,
}

/// <summary>
/// Everything the system knows about one kind of notification: who it is for, whether it is on
/// out of the box, how loudly it arrives, and what can be tuned about it.
/// </summary>
/// <param name="Modules">
/// Who is eligible to receive it. A notification only ever reaches someone holding one of these
/// modules, so a cashier is never shown a supplier alert.
/// </param>
/// <param name="DefaultEnabled">
/// Whether it is on before anyone touches the settings. Kept deliberately sparing: a bell that
/// lights up all day is one nobody reads by the end of the week.
/// </param>
/// <param name="DefaultThreshold">The starting value for whatever <paramref name="ThresholdUnit"/> measures.</param>
public sealed record NotificationDescriptor(
    NotificationType Type,
    string Name,
    string Description,
    IReadOnlyCollection<AppModule> Modules,
    NotificationSeverity Severity,
    bool DefaultEnabled,
    ThresholdUnit ThresholdUnit = ThresholdUnit.None,
    decimal? DefaultThreshold = null)
{
    /// <summary>True when this notification has a number an administrator can tune.</summary>
    public bool HasThreshold => ThresholdUnit != ThresholdUnit.None;
}
