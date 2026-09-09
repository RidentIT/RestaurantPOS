using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Settings.Commands.UpdateApprovalPinPolicy;

/// <summary>
/// How forgiving a mistyped approval PIN is at the till: how many tries before the terminal
/// pauses, and for how long.
/// </summary>
public sealed record UpdateApprovalPinPolicyCommand(int MaxAttempts, int LockoutMinutes)
    : IRequest<Result<RestaurantSettingsDto>>;

public sealed class UpdateApprovalPinPolicyCommandValidator : AbstractValidator<UpdateApprovalPinPolicyCommand>
{
    public UpdateApprovalPinPolicyCommandValidator()
    {
        RuleFor(x => x.MaxAttempts).InclusiveBetween(1, 10);
        RuleFor(x => x.LockoutMinutes).InclusiveBetween(1, 60);
    }
}

internal sealed class UpdateApprovalPinPolicyCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateApprovalPinPolicyCommand, Result<RestaurantSettingsDto>>
{
    public async Task<Result<RestaurantSettingsDto>> Handle(
        UpdateApprovalPinPolicyCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);

        settings.UpdateApprovalPinPolicy(request.MaxAttempts, request.LockoutMinutes);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(settings.ToDto());
    }
}
