using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.AddOrderItems;

/// <summary>A dish being added to a bill, with anything the customer asked for.</summary>
public sealed record AddOrderItemInput(Guid MenuItemId, int Quantity, string? SpecialInstructions);

/// <summary>
/// Adds dishes to a bill (POS-003, POS-004, POS-005). Deliberately needs no approval, at any
/// point in an order's life (BR-POS-005) — a customer ordering another drink is the most ordinary
/// thing that happens at a table, and making a cashier fetch a manager for it would be absurd.
/// Once the order is open each addition prints its own KOT (POS-014).
/// </summary>
public sealed record AddOrderItemsCommand(Guid OrderId, IReadOnlyCollection<AddOrderItemInput> Items)
    : IRequest<Result<OrderMutationDto>>;

public sealed class AddOrderItemsCommandValidator : AbstractValidator<AddOrderItemsCommand>
{
    public AddOrderItemsCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Add at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MenuItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            item.RuleFor(i => i.SpecialInstructions).MaximumLength(OrderItem.SpecialInstructionsMaxLength);
        });
    }
}

internal sealed class AddOrderItemsCommandHandler(
    IAppDbContext db, IDateTimeProvider clock)
    : IRequestHandler<AddOrderItemsCommand, Result<OrderMutationDto>>
{
    public async Task<Result<OrderMutationDto>> Handle(
        AddOrderItemsCommand request, CancellationToken cancellationToken)
    {
        var order = await OrderRepository.FindAsync(db, request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotFound(request.OrderId));
        }

        if (order.Status is not (OrderStatus.Draft or OrderStatus.Open))
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotEditable);
        }

        var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await db.MenuItems.AsNoTracking()
            .Where(m => menuItemIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        var missing = menuItemIds.FirstOrDefault(id => !menuItems.ContainsKey(id));
        if (missing != Guid.Empty)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.MenuItemNotFound(missing));
        }

        if (menuItems.Values.Any(m => !m.IsActive))
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.MenuItemInactive);
        }

        var newItems = request.Items
            .Select(i => new NewOrderItem(
                i.MenuItemId,
                menuItems[i.MenuItemId].Name,
                menuItems[i.MenuItemId].Price,
                i.Quantity,
                i.SpecialInstructions))
            .ToList();

        var ticket = order.AddItems(newItems, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await OrderResultFactory.BuildAsync(db, order, ticket, cancellationToken));
    }
}
