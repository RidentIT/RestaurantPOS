using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Orders.Queries.GetTables;

/// <summary>
/// The floor plan: every table with whatever order is sitting on it (POS-034). This is the
/// cashier's home screen, so it carries enough of each live order — item count, total, kitchen
/// progress — to decide where to go next without opening anything.
/// </summary>
public sealed record GetTablesQuery(bool? IsActive) : IRequest<Result<IReadOnlyCollection<TableDto>>>;

internal sealed class GetTablesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetTablesQuery, Result<IReadOnlyCollection<TableDto>>>
{
    private static readonly OrderStatus[] LiveStatuses =
        [OrderStatus.Draft, OrderStatus.Open, OrderStatus.Checkout];

    public async Task<Result<IReadOnlyCollection<TableDto>>> Handle(
        GetTablesQuery request, CancellationToken cancellationToken)
    {
        var tablesQuery = db.RestaurantTables.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            tablesQuery = tablesQuery.Where(t => t.IsActive == request.IsActive.Value);
        }

        var tables = await tablesQuery.ToListAsync(cancellationToken);

        var liveOrders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Tickets)
            .Where(o => LiveStatuses.Contains(o.Status))
            .ToListAsync(cancellationToken);

        var cashierNames = await db.Users.AsNoTracking()
            .Where(u => liveOrders.Select(o => o.CashierUserId).Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var ordersByTable = liveOrders.ToDictionary(o => o.TableId);

        var dtos = tables
            .Select(table =>
            {
                var order = ordersByTable.GetValueOrDefault(table.Id);

                var summary = order is null
                    ? null
                    : new TableOrderSummaryDto(
                        order.Id,
                        order.OrderNumber,
                        order.Status,
                        order.ActiveItems.Count(),
                        order.Total,
                        order.ConfirmedAtUtc,
                        cashierNames.GetValueOrDefault(order.CashierUserId, string.Empty),
                        OrderMappings.DeriveKitchenStatus(order.Tickets));

                return new TableDto(table.Id, table.Number, table.Seats, table.Notes, table.IsActive, summary);
            })
            // Numeric where the labels are numbers ("2" before "10"), alphabetical otherwise, so
            // the floor plan reads in the order staff walk the room.
            .OrderBy(t => int.TryParse(t.Number, out var n) ? n : int.MaxValue)
            .ThenBy(t => t.Number, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success<IReadOnlyCollection<TableDto>>(dtos);
    }
}
