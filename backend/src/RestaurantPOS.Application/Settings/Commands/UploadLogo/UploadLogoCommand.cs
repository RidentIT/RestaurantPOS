using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Settings.Commands.UploadLogo;

/// <summary>Replaces the restaurant's logo, shown on the sidebar, the sign-in screen and every receipt.</summary>
public sealed record UploadLogoCommand(Stream Content, string FileName, string ContentType, long SizeBytes)
    : IRequest<Result<RestaurantSettingsDto>>;

internal sealed class UploadLogoCommandHandler(IAppDbContext db, ILogoStore store)
    : IRequestHandler<UploadLogoCommand, Result<RestaurantSettingsDto>>
{
    private const int MaxMegabytes = 2;
    private const long MaxSizeBytes = MaxMegabytes * 1024L * 1024L;

    /// <summary>
    /// What a logo is allowed to be. Restricted to raster formats a browser and a thermal-printer
    /// render both handle without surprises — no PDF, no SVG.
    /// </summary>
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public async Task<Result<RestaurantSettingsDto>> Handle(
        UploadLogoCommand request, CancellationToken cancellationToken)
    {
        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            return Result.Failure<RestaurantSettingsDto>(SettingsErrors.LogoTypeNotAllowed);
        }

        if (request.SizeBytes > MaxSizeBytes)
        {
            return Result.Failure<RestaurantSettingsDto>(SettingsErrors.LogoTooLarge(MaxMegabytes));
        }

        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);
        var previousPath = settings.LogoPath;

        var storedPath = await store.SaveAsync(request.Content, request.FileName, cancellationToken);
        settings.SetLogo(storedPath);
        await db.SaveChangesAsync(cancellationToken);

        // Only once the new file is safely referenced by the settings row is the old one removed —
        // if the save above had failed, the previous logo would still be the one in use.
        if (!string.IsNullOrWhiteSpace(previousPath))
        {
            await store.DeleteAsync(previousPath, cancellationToken);
        }

        return Result.Success(settings.ToDto());
    }
}
