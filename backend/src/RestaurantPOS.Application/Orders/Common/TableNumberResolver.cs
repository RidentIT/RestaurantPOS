using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Application.Orders.Common;

/// <summary>
/// Resolves the one table number a single order needs displayed — null straight through for a
/// takeaway order, which never has one. Callers loading several orders at once resolve their own
/// bulk dictionary instead of calling this in a loop.
/// </summary>
public static class TableNumberResolver
{
    public static async Task<string?> ResolveAsync(IAppDbContext db, Guid? tableId, CancellationToken cancellationToken)
    {
        if (tableId is not { } id)
        {
            return null;
        }

        return await db.RestaurantTables
            .Where(t => t.Id == id)
            .Select(t => t.Number)
            .FirstAsync(cancellationToken);
    }
}
