using MediatR;

using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Modules;

namespace RestaurantPOS.Application.Users.Queries.GetModules;

/// <summary>
/// Returns the module catalog. The frontend builds both its navigation sidebar and the
/// per-user permission editor from this, so modules never have to be listed twice.
/// </summary>
public sealed record GetModulesQuery : IRequest<Result<IReadOnlyCollection<ModuleDto>>>;

/// <summary>
/// A module as presented to the client. <see cref="Module"/> serialises to its name (for
/// example <c>"PosBilling"</c>), which is the same value that appears in a user's granted
/// module list — so the client can match the two directly.
/// </summary>
public sealed record ModuleDto(
    AppModule Module,
    string Name,
    string Group,
    string Description,
    int SortOrder,
    bool AdminOnly);

internal sealed class GetModulesQueryHandler : IRequestHandler<GetModulesQuery, Result<IReadOnlyCollection<ModuleDto>>>
{
    public Task<Result<IReadOnlyCollection<ModuleDto>>> Handle(
        GetModulesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ModuleDto> modules =
        [
            .. ModuleCatalog.All.Select(d => new ModuleDto(
                d.Module,
                d.Name,
                d.Group,
                d.Description,
                d.SortOrder,
                d.AdminOnly)),
        ];

        return Task.FromResult(Result.Success(modules));
    }
}