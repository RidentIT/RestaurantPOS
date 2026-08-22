using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Notifications;

namespace RestaurantPOS.Application.Notifications.Dtos;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string TypeName,
    NotificationSeverity Severity,
    string Title,
    string? Body,
    string? Link,
    DateTime RaisedAtUtc,
    bool IsRead,
    /// <summary>Whether this should also raise a Windows notification for this user.</summary>
    bool DesktopEnabled);

/// <summary>The bell's contents: what to show, and how many are still unread.</summary>
public sealed record NotificationFeedDto(
    int UnreadCount, IReadOnlyCollection<NotificationDto> Notifications);

/// <summary>
/// One row on the settings screen: what the notification is, whether this user wants it, and the
/// number an administrator can tune.
/// </summary>
public sealed record NotificationPreferenceDto(
    NotificationType Type,
    string Name,
    string Description,
    NotificationSeverity Severity,
    IReadOnlyCollection<AppModule> Modules,
    bool IsEnabled,
    bool DesktopEnabled,
    bool IsDefault,
    bool HasThreshold,
    ThresholdUnit ThresholdUnit,
    decimal? Threshold);
