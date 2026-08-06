using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Common;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
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

        var consumed = await SaleStockConsumption.ApplyAsync(
            db,
            [new SoldItem(request.MenuItemId, request.QuantitySold)],
            currentUser.UserId!.Value,
            clock.UtcNow,
            cancellationToken);

        if (consumed.IsFailure)
        {
            return Result.Failure<ConsumptionResultDto>(consumed.Error);
        }

        // Not every menu item has a recipe, and a disabled one is not usable for a new sale —
        // neither is a failure, since the sale itself has already happened by the time this
        // runs. There is simply nothing to deduct.
        if (consumed.Value.Count == 0)
        {
            return Result.Success(NoDeduction);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ConsumptionResultDto(Deducted: true, consumed.Value));
    }
}