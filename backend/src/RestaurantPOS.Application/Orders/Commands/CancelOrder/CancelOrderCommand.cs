using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Authentication.Commands.VerifyApprovalPin;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.CancelOrder;

/// <summary>
/// Abandons an order without payment (POS-021, BR-POS-009), releasing the table. The kitchen gets
/// one cancellation slip covering everything still live on the bill.
/// </summary>
public sealed record CancelOrderCommand(Guid OrderId, string? Pin, string? Reason)
    : IRequest<Result<OrderMutationDto>>;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(250);
    }
}

internal sealed class CancelOrderCommandHandler(
    IAppDbContext db, IDateTimeProvider clock, ISender sender)
    : IRequestHandler<CancelOrderCommand, Result<OrderMutationDto>>
{
    public async Task<Result<OrderMutationDto>> Handle(
        CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await OrderRepository.FindAsync(db, request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotFound(request.OrderId));
        }

        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotCancellable);
        }

        // A draft has never reached the kitchen and holds nothing but the table, so binning it is
        // the cashier's own business. Once confirmed, food has been started and a numbered order
        // exists in the day's takings, so writing it off needs a manager (BR-POS-009).
        var approvedBy = order.CashierUserId;

        if (order.Status is OrderStatus.Open or OrderStatus.Checkout)
        {
            var approval = await sender.Send(
                new VerifyApprovalPinCommand(request.Pin ?? string.Empty, request.Reason ?? "Cancel order"),
                cancellationToken);

            if (approval.IsFailure)
            {
                return Result.Failure<OrderMutationDto>(approval.Error);
            }

            approvedBy = approval.Value.ApprovedByUserId;
        }

        var ticket = order.Cancel(approvedBy, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await OrderResultFactory.BuildAsync(db, order, ticket, cancellationToken));
    }
}
