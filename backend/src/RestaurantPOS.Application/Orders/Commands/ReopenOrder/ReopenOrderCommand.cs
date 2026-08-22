using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.ReopenOrder;

/// <summary>
/// Backs out of the payment screen — the customer decided on one more drink. Any tenders keyed in
/// but not completed are discarded, since the bill they were counted against is about to change.
/// </summary>
public sealed record ReopenOrderCommand(Guid OrderId) : IRequest<Result<OrderMutationDto>>;

public sealed class ReopenOrderCommandValidator : AbstractValidator<ReopenOrderCommand>
{
    public ReopenOrderCommandValidator() => RuleFor(x => x.OrderId).NotEmpty();
}

internal sealed class ReopenOrderCommandHandler(IAppDbContext db)
    : IRequestHandler<ReopenOrderCommand, Result<OrderMutationDto>>
{
    public async Task<Result<OrderMutationDto>> Handle(
        ReopenOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await OrderRepository.FindAsync(db, request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotFound(request.OrderId));
        }

        if (order.Status != OrderStatus.Checkout)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotInCheckout);
        }

        var abandoned = order.Payments.ToList();
        order.ReturnToOpen();
        db.OrderPayments.RemoveRange(abandoned);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await OrderResultFactory.BuildAsync(db, order, null, cancellationToken));
    }
}
