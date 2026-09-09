using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Settings.Commands.RemoveLogo;

/// <summary>Clears the restaurant's logo. The sidebar, sign-in screen and receipts fall back to initials.</summary>
public sealed record RemoveLogoCommand : IRequest<Result<RestaurantSettingsDto>>;

internal sealed class RemoveLogoCommandHandler(IAppDbContext db, ILogoStore store)
    : IRequestHandler<RemoveLogoCommand, Result<RestaurantSettingsDto>>
{
    public async Task<Result<RestaurantSettingsDto>> Handle(
        RemoveLogoCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);
        var storedPath = settings.LogoPath;

        settings.SetLogo(null);
        await db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(storedPath))
        {
            await store.DeleteAsync(storedPath, cancellationToken);
        }

        return Result.Success(settings.ToDto());
    }
}
