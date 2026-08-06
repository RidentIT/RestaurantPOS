using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Inventory.Common;

/// <summary>One menu item and how many of it were sold.</summary>
public sealed record SoldItem(Guid MenuItemId, decimal Quantity);

/// <summary>
/// Turns a completed sale into kitchen stock deductions, following each dish's recipe
/// (REC-007, INV-013).
/// </summary>
/// <remarks>
/// Shared by the single-item command and the till's settle-the-bill flow so the two can never
/// disagree about what a sale consumes. Everything for one bill is applied as a single batch,
/// which matters when two dishes share an ingredient: separate batches would write two movements
/// against a balance read at different moments.
/// </remarks>
public static class SaleStockConsumption
{
    /// <summary>
    /// Records the deductions for <paramref name="soldItems"/>. Dishes with no recipe, or whose
    /// recipe is switched off, simply consume nothing — that is a normal state for a bottled
    /// drink, not a failure.
    /// </summary>
    public static async Task<Result<IReadOnlyCollection<ConsumedLineDto>>> ApplyAsync(
        IAppDbContext db,
        IReadOnlyCollection<SoldItem> soldItems,
        Guid performedByUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(soldItems);

        var menuItemIds = soldItems.Select(s => s.MenuItemId).Distinct().ToList();

        var recipes = await db.Recipes.AsNoTracking()
            .Include(r => r.Lines)
            .Where(r => menuItemIds.Contains(r.MenuItemId) && r.IsEnabled)
            .ToListAsync(cancellationToken);

        if (recipes.Count == 0)
        {
            return Result.Success<IReadOnlyCollection<ConsumedLineDto>>([]);
        }

        var recipesByMenuItem = recipes.ToDictionary(r => r.MenuItemId);

        // Quantities are totalled per raw material first: one movement per ingredient reads far
        // better in the stock history than one per dish that happened to use it.
        var totals = new Dictionary<Guid, decimal>();

        foreach (var sold in soldItems)
        {
            if (!recipesByMenuItem.TryGetValue(sold.MenuItemId, out var recipe))
            {
                continue;
            }

            foreach (var line in recipe.Lines)
            {
                totals[line.RawMaterialId] = totals.GetValueOrDefault(line.RawMaterialId)
                    + (line.Quantity * sold.Quantity);
            }
        }

        if (totals.Count == 0)
        {
            return Result.Success<IReadOnlyCollection<ConsumedLineDto>>([]);
        }

        var rawMaterials = await db.RawMaterials.AsNoTracking()
            .Where(r => totals.Keys.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var movements = totals
            .Select(kv => new StockMovementRequest(
                kv.Key, StoreType.Kitchen, -kv.Value, StockMovementType.Consumption, ReferenceId: null, Notes: null))
            .ToList();

        // A sale is never refused for want of stock (BR-POS-015). The food has already gone out;
        // blocking the deduction would not un-cook it, it would only leave the ledger pretending
        // the ingredients are still on the shelf.
        var ledgerResult = await InventoryLedger.ApplyAsync(
            db, movements, performedByUserId, nowUtc, cancellationToken, allowNegative: true);

        if (ledgerResult.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<ConsumedLineDto>>(ledgerResult.Error);
        }

        var consumed = totals
            .Select(kv => new ConsumedLineDto(
                kv.Key, rawMaterials[kv.Key].Name, kv.Value, rawMaterials[kv.Key].UnitOfMeasurement))
            .OrderBy(l => l.RawMaterialName)
            .ToList();

        return Result.Success<IReadOnlyCollection<ConsumedLineDto>>(consumed);
    }
}
