using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Queries.GetOrderById;

/// <summary>Loads one bill in full, itemised as the cashier sees it on screen (POS-037).</summary>
public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderDto>>;

internal sealed class GetOrderByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await OrderRepository.WithAggregate(db.Orders.AsNoTracking())
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderDto>(OrderErrors.NotFound(request.OrderId));
        }

        var tableNumber = await TableNumberResolver.ResolveAsync(db, order.TableId, cancellationToken);

        var cashierName = await db.Users
            .Where(u => u.Id == order.CashierUserId)
            .Select(u => u.FullName)
            .FirstAsync(cancellationToken);

        return Result.Success(order.ToDto(tableNumber, cashierName));
    }
}
