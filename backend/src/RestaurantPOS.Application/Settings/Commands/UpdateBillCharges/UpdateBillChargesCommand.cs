using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Settings.Commands.UpdateBillCharges;

/// <summary>
/// Sets the VAT and service charge percentages a new bill will carry (BR-POS-011, BR-POS-012).
/// Orders already in progress keep whatever rate they were created with — this only changes what
/// the next order picks up.
/// </summary>
public sealed record UpdateBillChargesCommand(decimal TaxRatePercent, decimal ServiceChargeRatePercent)
    : IRequest<Result<RestaurantSettingsDto>>;

public sealed class UpdateBillChargesCommandValidator : AbstractValidator<UpdateBillChargesCommand>
{
    public UpdateBillChargesCommandValidator()
    {
        RuleFor(x => x.TaxRatePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.ServiceChargeRatePercent).InclusiveBetween(0, 100);
    }
}

internal sealed class UpdateBillChargesCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateBillChargesCommand, Result<RestaurantSettingsDto>>
{
    public async Task<Result<RestaurantSettingsDto>> Handle(
        UpdateBillChargesCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);

        settings.UpdateBillCharges(request.TaxRatePercent, request.ServiceChargeRatePercent);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(settings.ToDto());
    }
}
