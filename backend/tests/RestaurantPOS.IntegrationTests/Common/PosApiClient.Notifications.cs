using System.Net.Http.Json;

namespace RestaurantPOS.IntegrationTests.Common;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string TypeName,
    string Severity,
    string Title,
    string? Body,
    string? Link,
    bool IsRead,
    bool DesktopEnabled);

public sealed record NotificationFeedResponse(
    int UnreadCount, IReadOnlyCollection<NotificationResponse> Notifications);

public sealed record NotificationPreferenceResponse(
    string Type,
    string Name,
    string Description,
    string Severity,
    IReadOnlyCollection<string> Modules,
    bool IsEnabled,
    bool DesktopEnabled,
    bool IsDefault,
    bool HasThreshold,
    string ThresholdUnit,
    decimal? Threshold);

public sealed partial class PosApiClient
{
    public Task<HttpResponseMessage> GetNotificationsAsync(bool unreadOnly = false, int limit = 50) =>
        Http.GetAsync($"{BaseUrl}/notifications?unreadOnly={unreadOnly}&limit={limit}");

    public Task<HttpResponseMessage> EvaluateNotificationsAsync() =>
        Http.PostAsync($"{BaseUrl}/notifications/evaluate", null);

    public Task<HttpResponseMessage> MarkNotificationsReadAsync(params Guid[] notificationIds) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/notifications/read",
            new { notificationIds = notificationIds.Length == 0 ? null : notificationIds },
            Json);

    public Task<HttpResponseMessage> GetNotificationPreferencesAsync() =>
        Http.GetAsync($"{BaseUrl}/notifications/preferences");

    public Task<HttpResponseMessage> UpdateNotificationPreferenceAsync(
        string type, bool isEnabled, bool desktopEnabled = false) =>
        Http.PutAsJsonAsync(
            $"{BaseUrl}/notifications/preferences",
            new { type, isEnabled, desktopEnabled },
            Json);

    public Task<HttpResponseMessage> UpdateNotificationThresholdAsync(string type, decimal threshold) =>
        Http.PutAsJsonAsync($"{BaseUrl}/notifications/thresholds", new { type, threshold }, Json);
}
