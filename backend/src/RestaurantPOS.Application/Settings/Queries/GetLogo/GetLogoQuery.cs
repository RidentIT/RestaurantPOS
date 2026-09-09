using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Settings.Queries.GetLogo;

/// <summary>
/// Opens the restaurant's logo file. Deliberately reachable without signing in — the sign-in
/// screen itself is one of the places it's shown.
/// </summary>
public sealed record GetLogoQuery : IRequest<Result<StoredAttachment>>;

internal sealed class GetLogoQueryHandler(IAppDbContext db, ILogoStore store)
    : IRequestHandler<GetLogoQuery, Result<StoredAttachment>>
{
    public async Task<Result<StoredAttachment>> Handle(GetLogoQuery request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetAsync(db, cancellationToken);

        if (string.IsNullOrWhiteSpace(settings.LogoPath))
        {
            return Result.Failure<StoredAttachment>(SettingsErrors.LogoNotSet);
        }

        var stored = await store.OpenAsync(settings.LogoPath, cancellationToken);

        // The row survived but the file did not — someone tidied the folder, or a restore brought
        // back the database without the attachments beside it.
        return stored is null
            ? Result.Failure<StoredAttachment>(SettingsErrors.LogoMissing)
            : Result.Success(stored);
    }
}
