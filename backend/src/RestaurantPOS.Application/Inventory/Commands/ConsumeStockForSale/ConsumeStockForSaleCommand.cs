using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Common;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Inventory.Commands.ConsumeStockForSale;

/// <summary>
/// Deducts Kitchen stock per a menu item's recipe when a sale completes (REC-007, INV-013,
/// INV-014). This is the integration point POS &amp; Billing's "complete order" flow will call
/// once it exists; it is fully implemented and tested ahead of that.
/// </summary>
/// <param name="QuantitySold">How many units of the menu item were sold in this transaction.</param>
public sealed record ConsumeStockForSaleCommand(Guid MenuItemId, decimal QuantitySold)
    : IRequest<Result<ConsumptionResultDto>>;

public sealed class ConsumeStockForSaleCommandValidator : AbstractValidator<ConsumeStockForSaleCommand>
{
    public ConsumeStockForSaleCommandValidator()
    {
        RuleFor(x => x.MenuItemId).NotEmpty();
        RuleFor(x => x.QuantitySold).GreaterThan(0).WithMessage("Quantity sold must be greater than zero.");
    }
}

internal sealed class ConsumeStockForSaleCommandHandler(
    IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<ConsumeStockForSaleCommand, Result<ConsumptionResultDto>>
{
    private static readonly ConsumptionResultDto NoDeduction = new(Deducted: false, Lines: []);

    public async Task<Result<ConsumptionResultDto>> Handle(
        ConsumeStockForSaleCommand request, CancellationToken cancellationToken)
    {
        var menuItemExists = await db.MenuItems.AnyAsync(m => m.Id == request.MenuItemId, cancellationToken);
        if (!menuItemExists)
        {
            return Result.Failure<ConsumptionResultDto>(RecipeErrors.MenuItemNotFound(request.MenuItemId));
        }

        var recipe = await db.Recipes.AsNoTracking()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.MenuItemId == request.MenuItemId, cancellationToken);

        // Not every menu item has a recipe, and a disabled one is not usable for a new sale —
        // neither is a failure, since the sale itself has already happened by the time this
        // runs. There is simply nothing to deduct.
        if (recipe is null || !recipe.IsEnabled)
        {
            return Result.Success(NoDeduction);
        }

        var rawMaterialIds = recipe.Lines.Select(l => l.RawMaterialId).ToList();
        var rawMaterials = await db.RawMaterials.AsNoTracking()
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var now = clock.UtcNow;

        var movements = recipe.Lines
            .Select(l => new StockMovementRequest(
                l.RawMaterialId,
                StoreType.Kitchen,
                -(l.Quantity * request.QuantitySold),
                StockMovementType.Consumption,
                ReferenceId: null,
                Notes: null))
            .ToList();

        var ledgerResult = await InventoryLedger.ApplyAsync(db, movements, currentUser.UserId!.Value, now, cancellationToken);
        if (ledgerResult.IsFailure)
        {
            return Result.Failure<ConsumptionResultDto>(ledgerResult.Error);
        }

        await db.SaveChangesAsync(cancellationToken);

        var lines = movements
            .Select(m => new ConsumedLineDto(
                m.RawMaterialId, rawMaterials[m.RawMaterialId].Name, -m.QuantityDelta, rawMaterials[m.RawMaterialId].UnitOfMeasurement))
            .ToList();

        return Result.Success(new ConsumptionResultDto(Deducted: true, lines));
    }
}