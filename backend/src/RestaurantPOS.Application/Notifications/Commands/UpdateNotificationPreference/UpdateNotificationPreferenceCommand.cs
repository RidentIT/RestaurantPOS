using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;
using RestaurantPOS.Domain.Notifications;

namespace RestaurantPOS.Application.Notifications.Commands.UpdateNotificationPreference;

/// <summary>
/// Switches one notification on or off for the person asking. Personal: turning off "food ready
/// to serve" quietens your own bell, not everybody else's.
/// </summary>
public sealed record UpdateNotificationPreferenceCommand(
    NotificationType Type, bool IsEnabled, bool DesktopEnabled) : IRequest<Result>;

public sealed class UpdateNotificationPreferenceCommandValidator
    : AbstractValidator<UpdateNotificationPreferenceCommand>
{
    public UpdateNotificationPreferenceCommandValidator() => RuleFor(x => x.Type).IsInEnum();
}

internal sealed class UpdateNotificationPreferenceCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<UpdateNotificationPreferenceCommand, Result>
{
    public async Task<Result> Handle(
        UpdateNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        if (!NotificationCatalog.IsDefined(request.Type))
        {
            return Result.Failure(NotificationErrors.UnknownType);
        }

        var userId = currentUser.UserId!.Value;

        var existing = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == request.Type, cancellationToken);

        if (existing is null)
        {
            db.NotificationPreferences.Add(
                NotificationPreference.Create(userId, request.Type, request.IsEnabled, request.DesktopEnabled));
        }
        else
        {
            existing.Update(request.IsEnabled, request.DesktopEnabled);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
