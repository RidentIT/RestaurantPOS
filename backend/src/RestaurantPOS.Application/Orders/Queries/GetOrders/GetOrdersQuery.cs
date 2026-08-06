using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Orders.Queries.GetOrders;

/// <summary>
/// Lists orders for the dashboard and for searching (POS-034, POS-036).
/// </summary>
/// <param name="OpenOnly">Restricts to orders still holding a table — the dashboard's default.</param>
/// <param name="Search">Matches a table number or an order number, however the cashier remembers it.</param>
public sealed record GetOrdersQuery(bool OpenOnly, OrderStatus? Status, string? Search)
    : IRequest<Result<IReadOnlyCollection<OrderSummaryDto>>>;

internal sealed class GetOrdersQueryHandler(IAppDbContext db)
    : IRequestHandler<GetOrdersQuery, Result<IReadOnlyCollection<OrderSummaryDto>>>
{
    private static readonly OrderStatus[] LiveStatuses =
        [OrderStatus.Draft, OrderStatus.Open, OrderStatus.Checkout];

    public async Task<Result<IReadOnlyCollection<OrderSummaryDto>>> Handle(
        GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = OrderRepository.WithAggregate(db.Orders.AsNoTracking());

        if (request.OpenOnly)
        {
            query = query.Where(o => LiveStatuses.Contains(o.Status));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        var orders = await query.ToListAsync(cancellationToken);

        var tableNumbers = await db.RestaurantTables.AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.Number, cancellationToken);

        var cashierNames = await db.Users.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        var summaries = orders
            .Select(o => o.ToSummaryDto(
                tableNumbers.GetValueOrDefault(o.TableId, string.Empty),
                cashierNames.GetValueOrDefault(o.CashierUserId, string.Empty)))
            .ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();

            summaries = [.. summaries.Where(s =>
                s.TableNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (s.OrderNumber?.ToString().Contains(term, StringComparison.Ordinal) ?? false))];
        }

        // Newest first, but drafts and anything not yet numbered sort by when they were started.
        return Result.Success<IReadOnlyCollection<OrderSummaryDto>>(
            [.. summaries.OrderByDescending(s => s.CreatedAtUtc)]);
    }
}
