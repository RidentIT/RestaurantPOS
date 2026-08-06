using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Inventory.Common;

/// <summary>One requested change to one raw material's balance in one store.</summary>
public sealed record StockMovementRequest(
    Guid RawMaterialId,
    StoreType Store,
    decimal QuantityDelta,
    StockMovementType Type,
    Guid? ReferenceId,
    string? Notes);

/// <summary>
/// The single place every stock-affecting use case (GRN, release, adjustment, consumption) goes
/// through. Every quantity change is a <see cref="StockMovementRequest"/> recorded here as a
/// permanent <see cref="StockMovement"/> row, with the corresponding <see cref="StockLevel"/>
/// balance kept in step — this is the only code path allowed to touch either (BR-INV-006).
/// </summary>
public static class InventoryLedger
{
    /// <summary>
    /// Applies every movement in <paramref name="movements"/> as a single all-or-nothing batch:
    /// if any resulting balance would go negative, nothing is applied and the failure names the
    /// offending raw material.
    /// </summary>
    /// <param name="allowNegative">
    /// Lets balances fall below zero instead of rejecting the batch. Set only when recording
    /// something that has already physically happened and cannot be refused — a dish sold at the
    /// till must never be blocked because the ingredient ledger says the kitchen ran out
    /// (BR-POS-015). The resulting negative balance is a true reading: it says the kitchen has
    /// been cooking from stock nobody booked in, which is exactly what the manager needs to see.
    /// </param>
    public static async Task<Result> ApplyAsync(
        IAppDbContext db,
        IReadOnlyCollection<StockMovementRequest> movements,
        Guid performedByUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken,
        bool allowNegative = false)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(movements);

        if (movements.Count == 0)
        {
            return Result.Failure(InventoryErrors.EmptyLines);
        }

        var keys = movements.Select(m => (m.RawMaterialId, m.Store)).Distinct().ToList();

        var levels = new Dictionary<(Guid, StoreType), StockLevel>();

        foreach (var (rawMaterialId, store) in keys)
        {
            var level = await db.StockLevels
                .FirstOrDefaultAsync(l => l.RawMaterialId == rawMaterialId && l.Store == store, cancellationToken);

            if (level is null)
            {
                level = new StockLevel(rawMaterialId, store);
                db.StockLevels.Add(level);
            }

            levels[(rawMaterialId, store)] = level;
        }

        // Checked as one batch before anything is written, so a multi-line transaction never
        // applies some of its lines and then fails partway through the rest.
        var projectedBalances = levels.ToDictionary(kv => kv.Key, kv => kv.Value.QuantityOnHand);

        foreach (var movement in movements)
        {
            var key = (movement.RawMaterialId, movement.Store);
            var projected = projectedBalances[key] + movement.QuantityDelta;

            if (projected < 0 && !allowNegative)
            {
                return Result.Failure(InventoryErrors.InsufficientStock);
            }

            projectedBalances[key] = projected;
        }

        foreach (var movement in movements)
        {
            levels[(movement.RawMaterialId, movement.Store)].ApplyDelta(movement.QuantityDelta, nowUtc, allowNegative);

            db.StockMovements.Add(new StockMovement(
                movement.RawMaterialId,
                movement.Store,
                movement.QuantityDelta,
                movement.Type,
                movement.ReferenceId,
                performedByUserId,
                nowUtc,
                movement.Notes));
        }

        return Result.Success();
    }
}