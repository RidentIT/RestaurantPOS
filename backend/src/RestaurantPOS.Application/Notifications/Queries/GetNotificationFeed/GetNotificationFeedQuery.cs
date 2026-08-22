using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Notifications.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Notifications;

namespace RestaurantPOS.Application.Notifications.Queries.GetNotificationFeed;

/// <summary>
/// What is in this person's bell. Scoped to the caller — notifications are stored per recipient,
/// so there is nothing to filter and nothing to leak.
/// </summary>
/// <param name="UnreadOnly">Restricts to what has not been read yet.</param>
/// <param name="Limit">Caps the feed so the bell cannot try to render a year of history.</param>
public sealed record GetNotificationFeedQuery(bool UnreadOnly = false, int Limit = 50)
    : IRequest<Result<NotificationFeedDto>>;

internal sealed class GetNotificationFeedQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetNotificationFeedQuery, Result<NotificationFeedDto>>
{
    public async Task<Result<NotificationFeedDto>> Handle(
        GetNotificationFeedQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var limit = Math.Clamp(request.Limit, 1, 200);

        var query = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        if (request.UnreadOnly)
        {
            query = query.Where(n => n.ReadAtUtc == null);
        }

        var notifications = await query
            .OrderByDescending(n => n.RaisedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var unreadCount = await db.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && n.ReadAtUtc == null, cancellationToken);

        var desktopChoices = await db.NotificationPreferences.AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.Type, p => p.DesktopEnabled, cancellationToken);

        var dtos = notifications
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                NotificationCatalog.IsDefined(n.Type)
                    ? NotificationCatalog.Describe(n.Type).Name
                    : n.Type.ToString(),
                n.Severity,
                n.Title,
                n.Body,
                n.Link,
                n.RaisedAtUtc,
                n.IsRead,
                desktopChoices.GetValueOrDefault(n.Type)))
            .ToList();

        return Result.Success(new NotificationFeedDto(unreadCount, dtos));
    }
}
