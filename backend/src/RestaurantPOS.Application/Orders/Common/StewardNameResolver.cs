using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Application.Orders.Common;

/// <summary>
/// Resolves the name of the steward one order is credited to — null straight through when the
/// order has none (every takeaway, and any dine-in order not yet assigned). Callers loading many
/// orders resolve their own bulk lookup rather than calling this in a loop.
/// </summary>
public static class StewardNameResolver
{
    public static async Task<string?> ResolveAsync(
        IAppDbContext db, Guid? stewardId, CancellationToken cancellationToken)
    {
        if (stewardId is not { } id)
        {
            return null;
        }

        return await db.Stewards
            .Where(s => s.Id == id)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
