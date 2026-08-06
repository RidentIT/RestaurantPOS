using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Orders.Common;

/// <summary>
/// Assembles the response every order command returns: the order as it now stands, plus the
/// kitchen slip the change obliges — looking up the table number and cashier name each needs.
/// </summary>
public static class OrderResultFactory
{
    public static async Task<OrderMutationDto> BuildAsync(
        IAppDbContext db,
        Order order,
        KitchenTicket? ticket,
        IRestaurantProfile restaurant,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(restaurant);

        var tableNumber = await db.RestaurantTables
            .Where(t => t.Id == order.TableId)
            .Select(t => t.Number)
            .FirstAsync(cancellationToken);

        var cashierName = await db.Users
            .Where(u => u.Id == order.CashierUserId)
            .Select(u => u.FullName)
            .FirstAsync(cancellationToken);

        var kot = ticket?.ToKotDocument(order, restaurant.Name, tableNumber, cashierName);

        return new OrderMutationDto(order.ToDto(tableNumber, cashierName), kot);
    }
}
