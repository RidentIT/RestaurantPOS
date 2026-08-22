using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Settings.Queries.GetRestaurantSettings;

/// <summary>The Settings page's starting point: every configurable value as it stands today.</summary>
public sealed record GetRestaurantSettingsQuery : IRequest<Result<RestaurantSettingsDto>>;

internal sealed class GetRestaurantSettingsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetRestaurantSettingsQuery, Result<RestaurantSettingsDto>>
{
    public async Task<Result<RestaurantSettingsDto>> Handle(
        GetRestaurantSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetAsync(db, cancellationToken);

        return Result.Success(settings.ToDto());
    }
}
