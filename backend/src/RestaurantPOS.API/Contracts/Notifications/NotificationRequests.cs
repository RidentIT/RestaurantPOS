using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Contracts.Notifications;

/// <summary>An empty or omitted list means "everything of mine".</summary>
public sealed record MarkNotificationsReadRequest(IReadOnlyCollection<Guid>? NotificationIds);

public sealed record UpdateNotificationPreferenceRequest(
    NotificationType Type, bool IsEnabled, bool DesktopEnabled);

public sealed record UpdateNotificationThresholdRequest(NotificationType Type, decimal Threshold);
