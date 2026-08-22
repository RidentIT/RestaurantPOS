using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Notifications.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Notifications;

namespace RestaurantPOS.Application.Notifications.Queries.GetNotificationPreferences;

/// <summary>
/// The settings screen: every notification this person is eligible for, with their own choice and
/// the shared threshold. Notifications for modules they do not hold are left out entirely rather
/// than shown greyed out — an option you can never receive is just clutter.
/// </summary>
public sealed record GetNotificationPreferencesQuery
    : IRequest<Result<IReadOnlyCollection<NotificationPreferenceDto>>>;

internal sealed class GetNotificationPreferencesQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetNotificationPreferencesQuery, Result<IReadOnlyCollection<NotificationPreferenceDto>>>
{
    public async Task<Result<IReadOnlyCollection<NotificationPreferenceDto>>> Handle(
        GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var modules = await db.UserModulePermissions.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.Module)
            .ToListAsync(cancellationToken);

        var chosen = await db.NotificationPreferences.AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.Type, cancellationToken);

        var thresholds = await db.NotificationSettings.AsNoTracking()
            .ToDictionaryAsync(s => s.Type, s => s.Threshold, cancellationToken);

        var dtos = NotificationCatalog
            .ForModules(modules, currentUser.IsAdmin)
            .Select(descriptor =>
            {
                var preference = chosen.GetValueOrDefault(descriptor.Type);

                return new NotificationPreferenceDto(
                    descriptor.Type,
                    descriptor.Name,
                    descriptor.Description,
                    descriptor.Severity,
                    descriptor.Modules,
                    preference?.IsEnabled ?? descriptor.DefaultEnabled,
                    preference?.DesktopEnabled ?? false,
                    IsDefault: preference is null,
                    descriptor.HasThreshold,
                    descriptor.ThresholdUnit,
                    descriptor.HasThreshold
                        ? thresholds.GetValueOrDefault(descriptor.Type, descriptor.DefaultThreshold ?? 0m)
                        : null);
            })
            .ToList();

        return Result.Success<IReadOnlyCollection<NotificationPreferenceDto>>(dtos);
    }
}
