using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Users.Queries.GetUsers;

/// <summary>
/// Lists staff accounts for the administration screen, with optional filtering.
/// </summary>
/// <param name="Search">Matches against username or full name.</param>
/// <param name="Role">Restricts to a single role when supplied.</param>
/// <param name="IsActive">Restricts to active or inactive accounts when supplied.</param>
public sealed record GetUsersQuery(string? Search, UserRole? Role, bool? IsActive)
    : IRequest<Result<IReadOnlyCollection<UserDto>>>;

internal sealed class GetUsersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetUsersQuery, Result<IReadOnlyCollection<UserDto>>>
{
    public async Task<Result<IReadOnlyCollection<UserDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Users
            .AsNoTracking()
            .Include(u => u.ModulePermissions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Username.Contains(term) ||
                u.FullName.ToLower().Contains(term));
        }

        if (request.Role is not null)
        {
            query = query.Where(u => u.Role == request.Role.Value);
        }

        if (request.IsActive is not null)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        var users = await query
            .OrderByDescending(u => u.IsActive)
            .ThenBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<UserDto> result = [.. users.Select(u => u.ToDto())];

        return Result.Success(result);
    }
}