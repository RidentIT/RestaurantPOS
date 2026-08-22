using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Notifications.Commands.MarkNotificationsRead;

/// <summary>
/// Marks notifications as read. An empty list means "all of mine", which is what the
/// "mark all read" button sends.
/// </summary>
/// <remarks>
/// Silently ignores ids belonging to somebody else rather than refusing the whole request: they
/// are already invisible to this user, so there is nothing to reveal by failing loudly, and one
/// stale id from a background poll should not stop the rest being cleared.
/// </remarks>
public sealed record MarkNotificationsReadCommand(IReadOnlyCollection<Guid>? NotificationIds)
    : IRequest<Result<int>>;

internal sealed class MarkNotificationsReadCommandHandler(
    IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<MarkNotificationsReadCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var query = db.Notifications.Where(n => n.UserId == userId && n.ReadAtUtc == null);

        if (request.NotificationIds is { Count: > 0 } ids)
        {
            query = query.Where(n => ids.Contains(n.Id));
        }

        var notifications = await query.ToListAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            return Result.Success(0);
        }

        var now = clock.UtcNow;

        foreach (var notification in notifications)
        {
            notification.MarkRead(now);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(notifications.Count);
    }
}
