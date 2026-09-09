using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Kitchen.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Kitchen.Queries.GetKitchenTickets;

/// <summary>
/// The kitchen display: every slip still being worked, oldest first so the pass works the queue
/// in the order tickets landed.
/// </summary>
/// <param name="IncludeServed">
/// Adds tickets already carried out, scoped to today (see remarks). Off by default — the display
/// is a work queue, and finished tickets would push live ones off the screen.
/// </param>
/// <remarks>
/// "Today" rather than "ever": a wall display has no use for a KOT from three weeks ago, and
/// without a bound this would only get slower to load every day the restaurant keeps trading —
/// every ticket it has ever printed stays in the database (it's the kitchen's half of the order
/// record), but only today's are worth showing here. Anyone who genuinely needs to look further
/// back belongs in Reports, not the pass.
/// </remarks>
public sealed record GetKitchenTicketsQuery(bool IncludeServed)
    : IRequest<Result<IReadOnlyCollection<KitchenTicketDto>>>;

internal sealed class GetKitchenTicketsQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    : IRequestHandler<GetKitchenTicketsQuery, Result<IReadOnlyCollection<KitchenTicketDto>>>
{
    public async Task<Result<IReadOnlyCollection<KitchenTicketDto>>> Handle(
        GetKitchenTicketsQuery request, CancellationToken cancellationToken)
    {
        var query = db.KitchenTickets.AsNoTracking().Include(t => t.Lines).AsQueryable();

        if (!request.IncludeServed)
        {
            query = query.Where(t => t.Status != KitchenTicketStatus.Served);
        }
        else
        {
            var todayStartUtc = clock.Today.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
            var tomorrowStartUtc = clock.Today.AddDays(1).ToDateTime(TimeOnly.MinValue).ToUniversalTime();

            query = query.Where(t =>
                t.Status != KitchenTicketStatus.Served
                || (t.PrintedAtUtc >= todayStartUtc && t.PrintedAtUtc < tomorrowStartUtc));
        }

        var tickets = await query.ToListAsync(cancellationToken);

        var orderIds = tickets.Select(t => t.OrderId).Distinct().ToList();

        var orders = await db.Orders.AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .Select(o => new { o.Id, o.OrderNumber, o.TableId, o.Status })
            .ToListAsync(cancellationToken);

        var tableNumbers = await db.RestaurantTables.AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.Number, cancellationToken);

        var ordersById = orders.ToDictionary(o => o.Id);
        var now = clock.UtcNow;

        var dtos = tickets
            .Where(t => ordersById.ContainsKey(t.OrderId))
            .Select(t =>
            {
                var order = ordersById[t.OrderId];

                var tableNumber = order.TableId is { } tableId ? tableNumbers.GetValueOrDefault(tableId) : null;

                return t.ToDto(order.OrderNumber, tableNumber, now);
            })
            .OrderBy(t => t.PrintedAtUtc)
            .ToList();

        return Result.Success<IReadOnlyCollection<KitchenTicketDto>>(dtos);
    }
}
