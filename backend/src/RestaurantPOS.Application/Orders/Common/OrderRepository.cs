using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Orders.Common;

/// <summary>
/// The one way to load an order.
/// </summary>
/// <remarks>
/// Every collection on the aggregate sits behind a private backing field, so a handler that
/// loads an order without eager-loading them sees an empty bill and silently computes a total of
/// zero — no exception, just a wrong number. That failure has already been shipped twice in this
/// codebase on other aggregates, so loading is centralised here rather than left to each handler
/// to remember.
/// </remarks>
public static class OrderRepository
{
    /// <summary>Loads an order with everything the aggregate needs to compute totals and tickets.</summary>
    public static Task<Order?> FindAsync(IAppDbContext db, Guid orderId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        return WithAggregate(db.Orders).FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    /// <summary>Applies the full set of includes to an order query.</summary>
    public static IQueryable<Order> WithAggregate(IQueryable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        return orders
            .Include(o => o.Items)
            .Include(o => o.Tickets)
                .ThenInclude(t => t.Lines)
            .Include(o => o.Payments)
            .Include(o => o.Receipt);
    }
}
