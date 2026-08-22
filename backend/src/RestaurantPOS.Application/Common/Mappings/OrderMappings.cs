using RestaurantPOS.Application.Kitchen.Dtos;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Common.Mappings;

/// <summary>Projections from the order aggregate to the shapes the till and kitchen read.</summary>
public static class OrderMappings
{
    /// <summary>
    /// How far the kitchen has got with an order overall: the least-advanced ticket that still
    /// represents food to cook. Cancellation slips are skipped — they are notices, and letting
    /// one count would drag a finished table back to "new" the moment an item was voided.
    /// Null when nothing has been sent to the kitchen yet.
    /// </summary>
    public static KitchenTicketStatus? DeriveKitchenStatus(IEnumerable<KitchenTicket> tickets)
    {
        ArgumentNullException.ThrowIfNull(tickets);

        var workable = tickets.Where(t => t.IsWorkable).ToList();

        return workable.Count == 0 ? null : workable.Min(t => t.Status);
    }

    public static OrderDto ToDto(this Order order, string tableNumber, string cashierName)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.OrderDate,
            order.TableId,
            tableNumber,
            order.Status,
            order.CashierUserId,
            cashierName,
            order.CreatedAtUtc,
            order.ConfirmedAtUtc,
            order.CompletedAtUtc,
            order.DiscountType,
            order.DiscountValue,
            order.Subtotal,
            order.DiscountAmount,
            order.ServiceChargeRatePercent,
            order.ServiceChargeAmount,
            order.TaxRatePercent,
            order.TaxAmount,
            order.Total,
            order.AmountPaid,
            order.ChangeDue,
            DeriveKitchenStatus(order.Tickets),
            order.Receipt?.Number,
            [.. order.Items
                .OrderBy(i => i.CreatedAtUtc)
                .Select(i => new OrderItemDto(
                    i.Id, i.MenuItemId, i.MenuItemName, i.UnitPrice, i.Quantity,
                    i.SpecialInstructions, i.IsCancelled, i.LineTotal))],
            [.. order.Payments
                .OrderBy(p => p.CreatedAtUtc)
                .Select(p => p.ToDto())]);
    }

    public static OrderPaymentDto ToDto(this OrderPayment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return new OrderPaymentDto(
            payment.Id, payment.Method, payment.Amount, payment.TenderedAmount,
            payment.ChangeGiven, payment.Reference, payment.CreatedAtUtc);
    }

    public static OrderSummaryDto ToSummaryDto(this Order order, string tableNumber, string cashierName)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new OrderSummaryDto(
            order.Id,
            order.OrderNumber,
            order.TableId,
            tableNumber,
            order.Status,
            cashierName,
            order.CreatedAtUtc,
            order.ConfirmedAtUtc,
            order.ActiveItems.Count(),
            order.Total,
            DeriveKitchenStatus(order.Tickets));
    }

    /// <summary>Builds the printable slip for a ticket the kitchen has just been sent.</summary>
    public static KotDocumentDto ToKotDocument(
        this KitchenTicket ticket, Order order, string restaurantName, string tableNumber, string cashierName)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ArgumentNullException.ThrowIfNull(order);

        return new KotDocumentDto(
            ticket.Id,
            restaurantName,
            order.OrderNumber,
            tableNumber,
            ticket.TicketNumber,
            ticket.Kind,
            cashierName,
            ticket.PrintedAtUtc,
            ticket.PrintCount,
            [.. ticket.Lines.Select(l => new KotDocumentLineDto(
                l.MenuItemName, l.Quantity, l.SpecialInstructions, l.Note))]);
    }

    public static KitchenTicketDto ToDto(
        this KitchenTicket ticket, int? orderNumber, string tableNumber, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        return new KitchenTicketDto(
            ticket.Id,
            ticket.OrderId,
            orderNumber,
            tableNumber,
            ticket.TicketNumber,
            ticket.Kind,
            ticket.Status,
            ticket.PrintedAtUtc,
            ticket.StartedAtUtc,
            ticket.ReadyAtUtc,
            ticket.ServedAtUtc,
            ticket.PrintCount,
            Math.Max(0, (int)(nowUtc - ticket.PrintedAtUtc).TotalMinutes),
            [.. ticket.Lines.Select(l => new KitchenTicketLineDto(
                l.OrderItemId, l.MenuItemName, l.Quantity, l.SpecialInstructions, l.Note))]);
    }
}
