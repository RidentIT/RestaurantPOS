using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Authentication.Commands.VerifyApprovalPin;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.RemoveOrderItem;

/// <summary>
/// Takes a dish off a bill (POS-019, BR-POS-008). On an open order this voids the line and sends
/// the kitchen a cancellation slip (POS-020); on a draft it simply deletes it, since the kitchen
/// never heard about it. Kitchen stock is not credited back either way (BR-POS-014) — anything
/// already cooked has genuinely been used up.
/// </summary>
public sealed record RemoveOrderItemCommand(Guid OrderId, Guid OrderItemId, string? Pin)
    : IRequest<Result<OrderMutationDto>>;

public sealed class RemoveOrderItemCommandValidator : AbstractValidator<RemoveOrderItemCommand>
{
    public RemoveOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.OrderItemId).NotEmpty();
    }
}

internal sealed class RemoveOrderItemCommandHandler(
    IAppDbContext db, IDateTimeProvider clock, ISender sender)
    : IRequestHandler<RemoveOrderItemCommand, Result<OrderMutationDto>>
{
    public async Task<Result<OrderMutationDto>> Handle(
        RemoveOrderItemCommand request, CancellationToken cancellationToken)
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

        var item = order.Items.FirstOrDefault(i => i.Id == request.OrderItemId);

        if (item is null)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.ItemNotFound(request.OrderItemId));
        }

        if (item.IsCancelled)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotEditable);
        }

        if (order.Status == OrderStatus.Open)
        {
            var approval = await sender.Send(
                new VerifyApprovalPinCommand(request.Pin ?? string.Empty, "Remove item from order"), cancellationToken);

            if (approval.IsFailure)
            {
                return Result.Failure<OrderMutationDto>(approval.Error);
            }
        }

        var ticket = order.RemoveItem(request.OrderItemId, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await OrderResultFactory.BuildAsync(db, order, ticket, cancellationToken));
    }
}
