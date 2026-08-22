using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Notifications.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Notifications.Rules;

/// <summary>Alerts about what is happening on the floor and at the pass, right now.</summary>
internal static class ServiceNotificationRules
{
    public static async Task<IReadOnlyCollection<NotificationCandidate>> EvaluateAsync(
        IAppDbContext db, DateTime nowUtc, NotificationThresholds thresholds, CancellationToken cancellationToken)
    {
        var candidates = new List<NotificationCandidate>();
        var since = nowUtc.AddHours(-12);

        var liveOrders = await db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Open || o.Status == OrderStatus.Checkout)
            .Join(
                db.RestaurantTables.AsNoTracking(),
                order => order.TableId,
                table => table.Id,
                (order, table) => new { order.Id, order.OrderNumber, order.ConfirmedAtUtc, TableNumber = table.Number })
            .ToListAsync(cancellationToken);

        var liveOrderIds = liveOrders.Select(o => o.Id).ToList();

        var tickets = await db.KitchenTickets.AsNoTracking()
            .Where(t => liveOrderIds.Contains(t.OrderId))
            .Select(t => new { t.Id, t.OrderId, t.Status, t.Kind, t.PrintedAtUtc, t.TicketNumber })
            .ToListAsync(cancellationToken);

        var workableByOrder = tickets
            .Where(t => t.Kind != KitchenTicketKind.Cancellation)
            .GroupBy(t => t.OrderId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var order in liveOrders)
        {
            var orderTickets = workableByOrder.GetValueOrDefault(order.Id, []);

            // Everything plated and nothing still cooking: the food is sitting on the pass.
            if (orderTickets.Count > 0 && orderTickets.All(t => t.Status == KitchenTicketStatus.Ready))
            {
                candidates.Add(new NotificationCandidate(
                    NotificationType.FoodReadyToServe,
                    $"FoodReady:{order.Id}:{orderTickets.Count}",
                    $"Table {order.TableNumber} is ready to serve",
                    $"Order #{order.OrderNumber:000} is plated and waiting.",
                    $"/pos/orders/{order.Id}"));
            }

            var openMinutes = order.ConfirmedAtUtc is null
                ? 0
                : (nowUtc - order.ConfirmedAtUtc.Value).TotalMinutes;

            var tableLateAfter = thresholds.ElapsedFor(NotificationType.TableOpenTooLong);

            if (tableLateAfter > 0 && openMinutes >= tableLateAfter)
            {
                candidates.Add(new NotificationCandidate(
                    NotificationType.TableOpenTooLong,
                    $"TableOpenTooLong:{order.Id}",
                    $"Table {order.TableNumber} has been open a long time",
                    $"Order #{order.OrderNumber:000} was confirmed {(int)openMinutes} minutes ago and is not paid.",
                    $"/pos/orders/{order.Id}"));
            }
        }

        candidates.AddRange(EvaluateTickets(tickets
            .Select(t => (t.Id, t.OrderId, t.Status, t.Kind, t.PrintedAtUtc, t.TicketNumber))
            .ToList(),
            liveOrders.ToDictionary(o => o.Id, o => o.TableNumber),
            nowUtc,
            thresholds));

        candidates.AddRange(await CancelledOrdersAsync(db, since, cancellationToken));
        candidates.AddRange(await LargeDiscountsAsync(db, since, thresholds, cancellationToken));

        return candidates;
    }

    private static IEnumerable<NotificationCandidate> EvaluateTickets(
        IReadOnlyCollection<(Guid Id, Guid OrderId, KitchenTicketStatus Status, KitchenTicketKind Kind, DateTime PrintedAtUtc, int TicketNumber)> tickets,
        IReadOnlyDictionary<Guid, string> tableNumbers,
        DateTime nowUtc,
        NotificationThresholds thresholds)
    {
        var lateAfter = thresholds.ElapsedFor(NotificationType.KitchenTicketWaitingTooLong);

        foreach (var ticket in tickets)
        {
            if (ticket.Kind == KitchenTicketKind.Cancellation || ticket.Status == KitchenTicketStatus.Served)
            {
                continue;
            }

            var table = tableNumbers.GetValueOrDefault(ticket.OrderId, "?");
            var waiting = (nowUtc - ticket.PrintedAtUtc).TotalMinutes;

            if (ticket.Status == KitchenTicketStatus.New && waiting < 2)
            {
                yield return new NotificationCandidate(
                    NotificationType.NewKitchenTicket,
                    $"NewTicket:{ticket.Id}",
                    $"New ticket for table {table}",
                    $"KOT-{ticket.TicketNumber} has reached the pass.",
                    "/kitchen");
            }

            if (ticket.Status != KitchenTicketStatus.Ready && waiting >= lateAfter)
            {
                yield return new NotificationCandidate(
                    NotificationType.KitchenTicketWaitingTooLong,
                    $"TicketLate:{ticket.Id}",
                    $"Table {table} has been waiting {(int)waiting} minutes",
                    $"KOT-{ticket.TicketNumber} is still {ticket.Status.ToString().ToLowerInvariant()}.",
                    "/kitchen");
            }
        }
    }

    private static async Task<IReadOnlyCollection<NotificationCandidate>> CancelledOrdersAsync(
        IAppDbContext db, DateTime since, CancellationToken cancellationToken)
    {
        var cancelled = await db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Cancelled && o.CancelledAtUtc >= since && o.OrderNumber != null)
            .Join(
                db.RestaurantTables.AsNoTracking(),
                order => order.TableId,
                table => table.Id,
                (order, table) => new { order.Id, order.OrderNumber, TableNumber = table.Number })
            .ToListAsync(cancellationToken);

        return [.. cancelled.Select(o => new NotificationCandidate(
            NotificationType.OrderCancelled,
            $"OrderCancelled:{o.Id}",
            $"Order #{o.OrderNumber:000} on table {o.TableNumber} was cancelled",
            "The bill was written off and the kitchen told to stop.",
            "/pos"))];
    }

    private static async Task<IReadOnlyCollection<NotificationCandidate>> LargeDiscountsAsync(
        IAppDbContext db, DateTime since, NotificationThresholds thresholds, CancellationToken cancellationToken)
    {
        var minimum = thresholds.For(NotificationType.LargeDiscountApplied);

        var discounted = await db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed
                && o.CompletedAtUtc >= since
                && o.DiscountType != DiscountType.None)
            .Include(o => o.Items)
            .Join(
                db.RestaurantTables.AsNoTracking(),
                order => order.TableId,
                table => table.Id,
                (order, table) => new { Order = order, TableNumber = table.Number })
            .ToListAsync(cancellationToken);

        return [.. discounted
            // The discount is computed from the lines, so it can only be filtered after loading.
            .Where(x => x.Order.DiscountAmount >= minimum && minimum > 0)
            .Select(x => new NotificationCandidate(
                NotificationType.LargeDiscountApplied,
                $"LargeDiscount:{x.Order.Id}",
                $"{x.Order.DiscountAmount:0.00} discounted on table {x.TableNumber}",
                $"Order #{x.Order.OrderNumber:000} was reduced from "
                    + $"{x.Order.Subtotal:0.00} to {x.Order.Total:0.00}.",
                "/pos"))];
    }
}
