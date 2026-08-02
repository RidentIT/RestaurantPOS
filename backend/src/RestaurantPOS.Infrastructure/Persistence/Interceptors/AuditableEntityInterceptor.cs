using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps <see cref="BaseEntity.CreatedAtUtc"/> and <see cref="BaseEntity.UpdatedAtUtc"/> so
/// individual handlers never have to remember to.
/// </summary>
internal sealed class AuditableEntityInterceptor(IDateTimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Stamp(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Stamp(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    break;

                default:
                    break;
            }
        }
    }
}