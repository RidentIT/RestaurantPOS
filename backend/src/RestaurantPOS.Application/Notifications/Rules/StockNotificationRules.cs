using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Notifications.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Notifications.Rules;

/// <summary>Stock alerts, derived from the balances and thresholds already held on each material.</summary>
internal static class StockNotificationRules
{
    public static async Task<IReadOnlyCollection<NotificationCandidate>> EvaluateAsync(
        IAppDbContext db, DateTime nowUtc, NotificationThresholds thresholds, CancellationToken cancellationToken)
    {
        var candidates = new List<NotificationCandidate>();

        var levels = await db.StockLevels.AsNoTracking()
            .Join(
                db.RawMaterials.AsNoTracking().Where(r => r.IsActive),
                level => level.RawMaterialId,
                material => material.Id,
                (level, material) => new
                {
                    material.Id,
                    material.Name,
                    material.UnitOfMeasurement,
                    material.MainStoreReorderLevel,
                    material.KitchenParLevel,
                    level.Store,
                    level.QuantityOnHand,
                })
            .ToListAsync(cancellationToken);

        foreach (var level in levels)
        {
            // Negative first and on its own: it is a different problem from running low, and one
            // that only surfaces because sales are never blocked for want of stock.
            if (level.QuantityOnHand < 0)
            {
                candidates.Add(new NotificationCandidate(
                    NotificationType.KitchenStockNegative,
                    $"NegativeStock:{level.Store}:{level.Id}",
                    $"{level.Name} has gone negative in {Describe(level.Store)}",
                    $"The balance is {level.QuantityOnHand:0.##} {level.UnitOfMeasurement}. "
                        + "Dishes were sold against stock that was never booked in.",
                    LinkFor(level.Store)));

                continue;
            }

            var threshold = level.Store == StoreType.MainStore
                ? level.MainStoreReorderLevel
                : level.KitchenParLevel;

            if (threshold is not > 0 || level.QuantityOnHand > threshold)
            {
                continue;
            }

            var type = level.Store == StoreType.MainStore
                ? NotificationType.MainStoreLowStock
                : NotificationType.KitchenLowStock;

            candidates.Add(new NotificationCandidate(
                type,
                $"LowStock:{level.Store}:{level.Id}",
                $"{level.Name} is low in {Describe(level.Store)}",
                $"{level.QuantityOnHand:0.##} {level.UnitOfMeasurement} left, "
                    + $"{(level.Store == StoreType.MainStore ? "reorder" : "par")} level is {threshold:0.##}.",
                LinkFor(level.Store)));
        }

        candidates.AddRange(await RecentAdjustmentsAsync(db, nowUtc, cancellationToken));

        return candidates;
    }

    /// <summary>
    /// Adjustments made since the last evaluation window. Unlike a low balance, an adjustment is
    /// something that happened rather than something that is true, so it is bounded by time.
    /// </summary>
    private static async Task<IReadOnlyCollection<NotificationCandidate>> RecentAdjustmentsAsync(
        IAppDbContext db, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var since = nowUtc.AddHours(-24);

        var adjustments = await db.StockMovements.AsNoTracking()
            .Where(m => m.Type == StockMovementType.Adjustment && m.OccurredAtUtc >= since)
            .Join(
                db.RawMaterials.AsNoTracking(),
                movement => movement.RawMaterialId,
                material => material.Id,
                (movement, material) => new
                {
                    movement.Id,
                    movement.Store,
                    movement.QuantityDelta,
                    movement.PerformedByUserId,
                    movement.Notes,
                    material.Name,
                    material.UnitOfMeasurement,
                })
            .ToListAsync(cancellationToken);

        if (adjustments.Count == 0)
        {
            return [];
        }

        // The movement stores who made it as an id only, so the name is looked up separately.
        var userIds = adjustments.Select(a => a.PerformedByUserId).Distinct().ToList();

        var names = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return [.. adjustments.Select(a => new NotificationCandidate(
            NotificationType.StockAdjustmentRecorded,
            $"StockAdjustment:{a.Id}",
            $"{a.Name} adjusted by {a.QuantityDelta:+0.##;-0.##} {a.UnitOfMeasurement}",
            $"{names.GetValueOrDefault(a.PerformedByUserId, "Someone")} corrected the "
                + $"{Describe(a.Store)} balance by hand{(a.Notes is null ? "" : $": {a.Notes}")}.",
            LinkFor(a.Store)))];
    }

    private static string Describe(StoreType store) => store == StoreType.MainStore ? "Main Store" : "the Kitchen";

    private static string LinkFor(StoreType store) =>
        store == StoreType.MainStore ? "/inventory/main-store" : "/inventory/kitchen";
}
