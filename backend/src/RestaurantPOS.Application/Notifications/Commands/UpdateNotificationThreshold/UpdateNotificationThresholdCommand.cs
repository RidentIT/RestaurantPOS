using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;
using RestaurantPOS.Domain.Notifications;

namespace RestaurantPOS.Application.Notifications.Commands.UpdateNotificationThreshold;

/// <summary>
/// Tunes the number behind a notification — how many minutes is "too long", what counts as a
/// "large" discount. Restaurant-wide and administrator-only: how long a dish may sit on the pass
/// is a fact about the kitchen, not a matter of personal taste.
/// </summary>
public sealed record UpdateNotificationThresholdCommand(NotificationType Type, decimal Threshold)
    : IRequest<Result>;

public sealed class UpdateNotificationThresholdCommandValidator
    : AbstractValidator<UpdateNotificationThresholdCommand>
{
    public UpdateNotificationThresholdCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Threshold).GreaterThanOrEqualTo(0).WithMessage("A threshold cannot be negative.");
    }
}

internal sealed class UpdateNotificationThresholdCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateNotificationThresholdCommand, Result>
{
    public async Task<Result> Handle(
        UpdateNotificationThresholdCommand request, CancellationToken cancellationToken)
    {
        if (!NotificationCatalog.IsDefined(request.Type))
        {
            return Result.Failure(NotificationErrors.UnknownType);
        }

        if (!NotificationCatalog.Describe(request.Type).HasThreshold)
        {
            return Result.Failure(NotificationErrors.NoThreshold);
        }

        var existing = await db.NotificationSettings
            .FirstOrDefaultAsync(s => s.Type == request.Type, cancellationToken);

        if (existing is null)
        {
            db.NotificationSettings.Add(NotificationSetting.Create(request.Type, request.Threshold));
        }
        else
        {
            existing.UpdateThreshold(request.Threshold);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
