using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Settings.Queries.GetBranding;

/// <summary>The restaurant's name, for a sign-in screen. Everything else in Settings needs a session first.</summary>
public sealed record BrandingDto(string Name);

public sealed record GetBrandingQuery : IRequest<Result<BrandingDto>>;

internal sealed class GetBrandingQueryHandler(IAppDbContext db)
    : IRequestHandler<GetBrandingQuery, Result<BrandingDto>>
{
    public async Task<Result<BrandingDto>> Handle(GetBrandingQuery request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetAsync(db, cancellationToken);

        return Result.Success(new BrandingDto(settings.Name));
    }
}
