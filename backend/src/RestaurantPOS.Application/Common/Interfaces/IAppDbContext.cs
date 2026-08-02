using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>
/// The persistence surface the application layer is allowed to touch. Keeping handlers on
/// this abstraction rather than the concrete <c>AppDbContext</c> preserves the dependency
/// rule enforced by the architecture tests.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<UserModulePermission> UserModulePermissions { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}