using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Application.Settings.Common;

/// <summary>
/// Loads the restaurant's single settings row.
/// </summary>
/// <remarks>
/// The database seeder guarantees exactly one row exists before the API ever serves a request —
/// the same guarantee it makes for the administrator account — so every caller here can assume
/// the row is there rather than handling a "not configured" case that can't happen.
/// </remarks>
public static class RestaurantSettingsAccessor
{
    /// <summary>For read-only use: receipts, KOTs, order creation, PIN throttling.</summary>
    public static Task<Domain.Entities.RestaurantSettings> GetAsync(IAppDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        return db.RestaurantSettings.AsNoTracking().FirstAsync(cancellationToken);
    }

    /// <summary>For settings-update handlers that need to call a mutator and save.</summary>
    public static Task<Domain.Entities.RestaurantSettings> GetTrackedAsync(IAppDbContext db, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        return db.RestaurantSettings.FirstAsync(cancellationToken);
    }
}
