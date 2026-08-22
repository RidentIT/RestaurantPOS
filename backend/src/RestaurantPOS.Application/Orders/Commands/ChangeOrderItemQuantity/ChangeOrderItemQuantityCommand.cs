using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Authentication.Commands.VerifyApprovalPin;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.ChangeOrderItemQuantity;

/// <summary>
/// Changes how many of a dish were ordered (POS-015, BR-POS-007).
/// </summary>
/// <remarks>
/// The PIN is verified here rather than trusted from the till. A client-side check would be
/// decoration: anything that can call this endpoint could simply skip the prompt and send the
/// change. The approval only means something if the server is the one checking it.
/// <para>
/// A draft carries no PIN requirement — nothing has been sent to the kitchen and no bill exists
/// yet, so making a cashier fetch a manager to fix a mis-key while still taking the order would
/// be pure friction. Approval starts mattering once the order is open.
/// </para>
/// </remarks>
public sealed record ChangeOrderItemQuantityCommand(Guid OrderId, Guid OrderItemId, int Quantity, string? Pin)
    : IRequest<Result<OrderMutationDto>>;

public sealed class ChangeOrderItemQuantityCommandValidator : AbstractValidator<ChangeOrderItemQuantityCommand>
{
    public ChangeOrderItemQuantityCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.OrderItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}

internal sealed class ChangeOrderItemQuantityCommandHandler(
    IAppDbContext db, IDateTimeProvider clock, ISender sender)
    : IRequestHandler<ChangeOrderItemQuantityCommand, Result<OrderMutationDto>>
{
    public async Task<Result<OrderMutationDto>> Handle(
        ChangeOrderItemQuantityCommand request, CancellationToken cancellationToken)
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

        if (!order.Items.Any(i => i.Id == request.OrderItemId))
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.ItemNotFound(request.OrderItemId));
        }

        if (order.Status == OrderStatus.Open)
        {
            var approval = await sender.Send(
                new VerifyApprovalPinCommand(request.Pin ?? string.Empty, "Change item quantity"), cancellationToken);

            if (approval.IsFailure)
            {
                return Result.Failure<OrderMutationDto>(approval.Error);
            }
        }

        var ticket = order.ChangeItemQuantity(request.OrderItemId, request.Quantity, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await OrderResultFactory.BuildAsync(db, order, ticket, cancellationToken));
    }
}
