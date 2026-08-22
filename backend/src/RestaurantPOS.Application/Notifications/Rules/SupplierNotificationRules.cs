using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Notifications.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Notifications.Rules;

/// <summary>Alerts about suppliers: late deliveries, money owing, problems and price rises.</summary>
internal static class SupplierNotificationRules
{
    public static async Task<IReadOnlyCollection<NotificationCandidate>> EvaluateAsync(
        IAppDbContext db, DateTime nowUtc, DateOnly today, NotificationThresholds thresholds,
        CancellationToken cancellationToken)
    {
        var candidates = new List<NotificationCandidate>();

        var orders = await db.PurchaseOrders.AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.Status != PurchaseOrderStatus.Draft && o.Status != PurchaseOrderStatus.Cancelled)
            .Join(
                db.Suppliers.AsNoTracking(),
                order => order.SupplierId,
                supplier => supplier.Id,
                (order, supplier) => new { Order = order, supplier.Name, supplier.PaymentTermsDays })
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(x => x.Order.Id).ToList();

        var paidByOrder = await db.SupplierPayments.AsNoTracking()
            .Where(p => orderIds.Contains(p.PurchaseOrderId))
            .GroupBy(p => p.PurchaseOrderId)
            .Select(g => new { OrderId = g.Key, Paid = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(g => g.OrderId, g => g.Paid, cancellationToken);

        var notConfirmedAfter = thresholds.WholeFor(NotificationType.PurchaseOrderNotConfirmed);

        foreach (var entry in orders)
        {
            var order = entry.Order;

            if (order.Status == PurchaseOrderStatus.Confirmed
                && order.ExpectedDeliveryDate is { } expected
                && DateOnly.FromDateTime(expected) < today)
            {
                var daysLate = today.DayNumber - DateOnly.FromDateTime(expected).DayNumber;

                candidates.Add(new NotificationCandidate(
                    NotificationType.PurchaseOrderDeliveryOverdue,
                    $"PoOverdue:{order.Id}",
                    $"Delivery from {entry.Name} is {daysLate} day{(daysLate == 1 ? "" : "s")} late",
                    $"Expected {expected:yyyy-MM-dd}, worth {order.TotalAmount:0.00}.",
                    "/suppliers"));
            }

            if (order.Status == PurchaseOrderStatus.Submitted
                && order.SubmittedAtUtc is { } submitted
                && notConfirmedAfter > 0
                && (nowUtc - submitted).TotalDays >= notConfirmedAfter)
            {
                candidates.Add(new NotificationCandidate(
                    NotificationType.PurchaseOrderNotConfirmed,
                    $"PoUnconfirmed:{order.Id}",
                    $"{entry.Name} has not confirmed an order",
                    $"Sent {(int)(nowUtc - submitted).TotalDays} days ago, worth {order.TotalAmount:0.00}.",
                    "/suppliers"));
            }

            if (order.Status == PurchaseOrderStatus.Delivered)
            {
                var balance = order.TotalAmount - paidByOrder.GetValueOrDefault(order.Id);

                // Payment is only "due" once the supplier's own terms have run out.
                var dueFrom = order.ExpectedDeliveryDate ?? order.CreatedAtUtc;

                if (balance > 0 && (nowUtc - dueFrom).TotalDays >= entry.PaymentTermsDays)
                {
                    candidates.Add(new NotificationCandidate(
                        NotificationType.SupplierPaymentDue,
                        $"SupplierPaymentDue:{order.Id}",
                        $"{balance:0.00} owing to {entry.Name}",
                        entry.PaymentTermsDays == 0
                            ? "This order was payable on delivery."
                            : $"Payment terms are {entry.PaymentTermsDays} days.",
                        "/suppliers"));
                }
            }
        }

        candidates.AddRange(await DeliveryProblemsAsync(db, nowUtc, cancellationToken));
        candidates.AddRange(await PriceRisesAsync(db, nowUtc, thresholds, cancellationToken));

        return candidates;
    }

    private static async Task<IReadOnlyCollection<NotificationCandidate>> DeliveryProblemsAsync(
        IAppDbContext db, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var since = nowUtc.AddDays(-7);

        var problems = await db.GoodsReceivedNotes.AsNoTracking()
            .Where(g => g.ReceivedAtUtc >= since && (g.HasIssue || (g.QualityRating != null && g.QualityRating <= 2)))
            .Join(
                db.Suppliers.AsNoTracking(),
                note => note.SupplierId,
                supplier => supplier.Id,
                (note, supplier) => new { note.Id, note.HasIssue, note.QualityRating, supplier.Name })
            .ToListAsync(cancellationToken);

        return [.. problems.Select(p => new NotificationCandidate(
            NotificationType.GoodsReceivedIssue,
            $"GrnIssue:{p.Id}",
            $"Problem with a delivery from {p.Name}",
            p.HasIssue
                ? $"Flagged with an issue{(p.QualityRating is { } r ? $", rated {r}/5" : "")}."
                : $"Rated {p.QualityRating}/5.",
            "/inventory/main-store"))];
    }

    private static async Task<IReadOnlyCollection<NotificationCandidate>> PriceRisesAsync(
        IAppDbContext db, DateTime nowUtc, NotificationThresholds thresholds, CancellationToken cancellationToken)
    {
        var minimumRise = thresholds.For(NotificationType.SupplierPriceIncreased);

        if (minimumRise <= 0)
        {
            return [];
        }

        var since = nowUtc.AddDays(-30);

        // The two most recent prices per supplier and material are what a "rise" is measured
        // between, so the whole recent window is loaded and paired up in memory.
        var history = await db.SupplierPriceHistoryEntries.AsNoTracking()
            .Where(h => h.RecordedAtUtc >= since)
            .ToListAsync(cancellationToken);

        var names = await db.Suppliers.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var materials = await db.RawMaterials.AsNoTracking()
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var candidates = new List<NotificationCandidate>();

        foreach (var group in history.GroupBy(h => (h.SupplierId, h.RawMaterialId)))
        {
            var ordered = group.OrderByDescending(h => h.RecordedAtUtc).ToList();

            if (ordered.Count < 2)
            {
                continue;
            }

            var latest = ordered[0];
            var previous = ordered[1];

            if (previous.Price <= 0 || latest.Price <= previous.Price)
            {
                continue;
            }

            var rise = (latest.Price - previous.Price) / previous.Price * 100m;

            if (rise < minimumRise)
            {
                continue;
            }

            candidates.Add(new NotificationCandidate(
                NotificationType.SupplierPriceIncreased,
                $"PriceRise:{latest.SupplierId}:{latest.RawMaterialId}:{latest.RecordedAtUtc.Ticks}",
                $"{names.GetValueOrDefault(latest.SupplierId, "A supplier")} raised a price by {rise:0.#}%",
                $"{materials.GetValueOrDefault(latest.RawMaterialId, "An item")} went from "
                    + $"{previous.Price:0.00} to {latest.Price:0.00}.",
                "/suppliers"));
        }

        return candidates;
    }
}
