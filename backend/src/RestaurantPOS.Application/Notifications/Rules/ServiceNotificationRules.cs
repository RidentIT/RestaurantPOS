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

        // Resolved once and threaded through every query below — a takeaway order has no table to
        // join against, so every lookup here is a bulk dictionary read rather than an INNER JOIN,
        // which would otherwise silently drop it from every alert in this file.
        var tableNumbers = await db.RestaurantTables.AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.Number, cancellationToken);

        var liveOrders = await db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Open || o.Status == OrderStatus.Checkout)
            .Select(o => new { o.Id, o.OrderNumber, o.ConfirmedAtUtc, o.TableId })
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
            var tableNumber = order.TableId is { } tableId ? tableNumbers.GetValueOrDefault(tableId) : null;
            var subject = Subject(tableNumber, order.OrderNumber, capitalized: true);

            var orderTickets = workableByOrder.GetValueOrDefault(order.Id, []);

            // Everything plated and nothing still cooking: the food is sitting on the pass.
            if (orderTickets.Count > 0 && orderTickets.All(t => t.Status == KitchenTicketStatus.Ready))
            {
                candidates.Add(new NotificationCandidate(
                    NotificationType.FoodReadyToServe,
                    $"FoodReady:{order.Id}:{orderTickets.Count}",
                    $"{subject} is ready to serve",
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
                    $"{subject} has been open a long time",
                    $"Order #{order.OrderNumber:000} was confirmed {(int)openMinutes} minutes ago and is not paid.",
                    $"/pos/orders/{order.Id}"));
            }
        }

        candidates.AddRange(EvaluateTickets(
            tickets.Select(t => (t.Id, t.OrderId, t.Status, t.Kind, t.PrintedAtUtc, t.TicketNumber)).ToList(),
            liveOrders.ToDictionary(
                o => o.Id,
                o => (TableNumber: o.TableId is { } tableId ? tableNumbers.GetValueOrDefault(tableId) : null, o.OrderNumber)),
            nowUtc,
            thresholds));

        candidates.AddRange(await CancelledOrdersAsync(db, tableNumbers, since, cancellationToken));
        candidates.AddRange(await LargeDiscountsAsync(db, tableNumbers, since, thresholds, cancellationToken));

        return candidates;
    }

    /// <summary>"Table 4" for a dine-in order, "Takeaway order #007" for one with no table.</summary>
    private static string Subject(string? tableNumber, int? orderNumber, bool capitalized = false) =>
        tableNumber is not null
            ? $"{(capitalized ? "Table" : "table")} {tableNumber}"
            : $"{(capitalized ? "Takeaway" : "takeaway")} order #{orderNumber:000}";

    private static IEnumerable<NotificationCandidate> EvaluateTickets(
        IReadOnlyCollection<(Guid Id, Guid OrderId, KitchenTicketStatus Status, KitchenTicketKind Kind, DateTime PrintedAtUtc, int TicketNumber)> tickets,
        IReadOnlyDictionary<Guid, (string? TableNumber, int? OrderNumber)> orders,
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

            var (tableNumber, orderNumber) = orders.GetValueOrDefault(ticket.OrderId, (null, null));
            var subject = Subject(tableNumber, orderNumber, capitalized: true);
            var waiting = (nowUtc - ticket.PrintedAtUtc).TotalMinutes;

            if (ticket.Status == KitchenTicketStatus.New && waiting < 2)
            {
                yield return new NotificationCandidate(
                    NotificationType.NewKitchenTicket,
                    $"NewTicket:{ticket.Id}",
                    $"New ticket for {Subject(tableNumber, orderNumber)}",
                    $"KOT-{ticket.TicketNumber} has reached the pass.",
                    "/kitchen");
            }

            if (ticket.Status != KitchenTicketStatus.Ready && waiting >= lateAfter)
            {
                yield return new NotificationCandidate(
                    NotificationType.KitchenTicketWaitingTooLong,
                    $"TicketLate:{ticket.Id}",
                    $"{subject} has been waiting {(int)waiting} minutes",
                    $"KOT-{ticket.TicketNumber} is still {ticket.Status.ToString().ToLowerInvariant()}.",
                    "/kitchen");
            }
        }
    }

    private static async Task<IReadOnlyCollection<NotificationCandidate>> CancelledOrdersAsync(
        IAppDbContext db, IReadOnlyDictionary<Guid, string> tableNumbers, DateTime since, CancellationToken cancellationToken)
    {
        var cancelled = await db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Cancelled && o.CancelledAtUtc >= since && o.OrderNumber != null)
            .Select(o => new { o.Id, o.OrderNumber, o.TableId })
            .ToListAsync(cancellationToken);

        return [.. cancelled.Select(o =>
        {
            var tableNumber = o.TableId is { } tableId ? tableNumbers.GetValueOrDefault(tableId) : null;

            return new NotificationCandidate(
                NotificationType.OrderCancelled,
                $"OrderCancelled:{o.Id}",
                $"Order #{o.OrderNumber:000} on {Subject(tableNumber, o.OrderNumber)} was cancelled",
                "The bill was written off and the kitchen told to stop.",
                "/pos");
        })];
    }

    private static async Task<IReadOnlyCollection<NotificationCandidate>> LargeDiscountsAsync(
        IAppDbContext db,
        IReadOnlyDictionary<Guid, string> tableNumbers,
        DateTime since,
        NotificationThresholds thresholds,
        CancellationToken cancellationToken)
    {
        var minimum = thresholds.For(NotificationType.LargeDiscountApplied);

        var discounted = await db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed
                && o.CompletedAtUtc >= since
                && o.DiscountType != DiscountType.None)
            .Include(o => o.Items)
            .ToListAsync(cancellationToken);

        return [.. discounted
            // The discount is computed from the lines, so it can only be filtered after loading.
            .Where(o => o.DiscountAmount >= minimum && minimum > 0)
            .Select(o =>
            {
                var tableNumber = o.TableId is { } tableId ? tableNumbers.GetValueOrDefault(tableId) : null;

                return new NotificationCandidate(
                    NotificationType.LargeDiscountApplied,
                    $"LargeDiscount:{o.Id}",
                    $"{o.DiscountAmount:0.00} discounted on {Subject(tableNumber, o.OrderNumber)}",
                    $"Order #{o.OrderNumber:000} was reduced from {o.Subtotal:0.00} to {o.Total:0.00}.",
                    "/pos");
            })];
    }
}
