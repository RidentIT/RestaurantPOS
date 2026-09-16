using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Settings.Queries.GetFrequentMenuCategories;

/// <summary>The categories pinned to the front of the till's category strip, add order first.</summary>
public sealed record GetFrequentMenuCategoriesQuery : IRequest<Result<IReadOnlyList<string>>>;

internal sealed class GetFrequentMenuCategoriesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetFrequentMenuCategoriesQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(
        GetFrequentMenuCategoriesQuery request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetAsync(db, cancellationToken);

        return Result.Success(settings.FrequentMenuCategories);
    }
}
