namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// How loudly a notification should arrive. This is the only thing that decides whether something
/// interrupts, so it is kept deliberately coarse — a scale with five levels ends up with
/// everything set to the top one.
/// </summary>
public enum NotificationSeverity
{
    /// <summary>Worth knowing. Appears in the feed and nowhere else.</summary>
    Info = 1,

    /// <summary>Needs attention before long. Feed, with the bell marked.</summary>
    Warning = 2,

    /// <summary>Needs attention now. Also raises a toast on screen.</summary>
    Urgent = 3,
}
