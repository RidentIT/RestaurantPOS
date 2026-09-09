using MediatR;

using RestaurantPOS.API.Contracts.Notifications;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Notifications.Commands.EvaluateNotifications;
using RestaurantPOS.Application.Notifications.Commands.MarkNotificationsRead;
using RestaurantPOS.Application.Notifications.Commands.UpdateNotificationPreference;
using RestaurantPOS.Application.Notifications.Commands.UpdateNotificationThreshold;
using RestaurantPOS.Application.Notifications.Queries.GetNotificationFeed;
using RestaurantPOS.Application.Notifications.Queries.GetNotificationPreferences;

namespace RestaurantPOS.API.Endpoints;

/// <summary>The notification bell, its feed and its settings.</summary>
public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        // Deliberately not gated on the Notifications module: a cashier who has not been granted
        // it still needs to be told their food is ready. What each person actually receives is
        // decided by the modules they hold, inside the dispatcher.
        var group = routes.MapGroup("/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", async (bool? unreadOnly, int? limit, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetNotificationFeedQuery(unreadOnly ?? false, limit ?? 50), ct);
                return result.ToHttpResult();
            })
            .WithName("GetNotifications")
            .WithSummary("The signed-in user's notifications, newest first.");

        group.MapPost("/evaluate", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new EvaluateNotificationsCommand(), ct);
                return result.ToHttpResult();
            })
            .WithName("EvaluateNotifications")
            .WithSummary("Works out what is worth raising right now. Called by the bell as it polls.");

        group.MapPost("/read", async (
                MarkNotificationsReadRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new MarkNotificationsReadCommand(request.NotificationIds), ct);
                return result.ToHttpResult();
            })
            .WithName("MarkNotificationsRead")
            .WithSummary("Marks notifications as read. Omit the list to clear everything.");

        group.MapGet("/preferences", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetNotificationPreferencesQuery(), ct);
                return result.ToHttpResult();
            })
            .WithName("GetNotificationPreferences")
            .WithSummary("Every notification this user is eligible for, with their own choices.");

        group.MapPut("/preferences", async (
                UpdateNotificationPreferenceRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateNotificationPreferenceCommand(
                    request.Type, request.IsEnabled, request.DesktopEnabled);

                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdateNotificationPreference")
            .WithSummary("Switches one notification on or off for the signed-in user.");

        group.MapPut("/thresholds", async (
                UpdateNotificationThresholdRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new UpdateNotificationThresholdCommand(request.Type, request.Threshold), ct);

                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("UpdateNotificationThreshold")
            .WithSummary("Tunes a notification's threshold for the whole restaurant.");

        return routes;
    }
}
