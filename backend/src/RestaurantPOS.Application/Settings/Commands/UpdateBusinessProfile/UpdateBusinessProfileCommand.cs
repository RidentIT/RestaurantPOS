using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Settings.Commands.UpdateBusinessProfile;

/// <summary>What goes at the top of every receipt and KOT (POS-027).</summary>
public sealed record UpdateBusinessProfileCommand(
    string Name,
    string AddressLine1,
    string? AddressLine2,
    string? City,
    string? Phone,
    string? LogoPath,
    string? VatRegistrationNumber)
    : IRequest<Result<RestaurantSettingsDto>>;

public sealed class UpdateBusinessProfileCommandValidator : AbstractValidator<UpdateBusinessProfileCommand>
{
    public UpdateBusinessProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(RestaurantSettings.NameMaxLength);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(RestaurantSettings.AddressLineMaxLength);
        RuleFor(x => x.AddressLine2).MaximumLength(RestaurantSettings.AddressLineMaxLength);
        RuleFor(x => x.City).MaximumLength(RestaurantSettings.CityMaxLength);
        RuleFor(x => x.Phone).MaximumLength(RestaurantSettings.PhoneMaxLength);
        RuleFor(x => x.LogoPath).MaximumLength(RestaurantSettings.LogoPathMaxLength);
        RuleFor(x => x.VatRegistrationNumber).MaximumLength(RestaurantSettings.VatRegistrationNumberMaxLength);
    }
}

internal sealed class UpdateBusinessProfileCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateBusinessProfileCommand, Result<RestaurantSettingsDto>>
{
    public async Task<Result<RestaurantSettingsDto>> Handle(
        UpdateBusinessProfileCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);

        settings.UpdateProfile(
            request.Name,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Phone,
            request.LogoPath,
            request.VatRegistrationNumber);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(settings.ToDto());
    }
}
