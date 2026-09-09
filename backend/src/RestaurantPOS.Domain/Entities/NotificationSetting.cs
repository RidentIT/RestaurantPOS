using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// The tunable number behind a notification — how many minutes is "too long", what counts as a
/// "large" discount.
/// </summary>
/// <remarks>
/// Restaurant-wide rather than personal. How long a dish may sit on the pass is a fact about the
/// kitchen, not a matter of taste, and letting each person hold their own answer would mean two
/// staff disagreeing about whether an order is late.
/// </remarks>
public sealed class NotificationSetting : BaseEntity
{
    // EF Core materialisation.
    private NotificationSetting()
    {
    }

    private NotificationSetting(NotificationType type, decimal threshold)
    {
        Type = type;
        Threshold = ValidateThreshold(threshold);
    }

    public NotificationType Type { get; private set; }

    public decimal Threshold { get; private set; }

    public static NotificationSetting Create(NotificationType type, decimal threshold) =>
        new(type, threshold);

    public void UpdateThreshold(decimal threshold) => Threshold = ValidateThreshold(threshold);

    private static decimal ValidateThreshold(decimal threshold) =>
        threshold >= 0
            ? threshold
            : throw new ArgumentOutOfRangeException(
                nameof(threshold), threshold, "A notification threshold cannot be negative.");
}
