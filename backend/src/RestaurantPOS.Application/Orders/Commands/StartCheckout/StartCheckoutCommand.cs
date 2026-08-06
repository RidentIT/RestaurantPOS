using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.StartCheckout;

/// <summary>
/// Moves a table to the payment screen, freezing the bill so it cannot change while the customer
/// is counting out money (POS-038 — one table settled per transaction).
/// </summary>
public sealed record StartCheckoutCommand(Guid OrderId) : IRequest<Result<OrderMutationDto>>;

public sealed class StartCheckoutCommandValidator : AbstractValidator<StartCheckoutCommand>
{
    public StartCheckoutCommandValidator() => RuleFor(x => x.OrderId).NotEmpty();
}

internal sealed class StartCheckoutCommandHandler(IAppDbContext db, IRestaurantProfile restaurant)
    : IRequestHandler<StartCheckoutCommand, Result<OrderMutationDto>>
{
    public async Task<Result<OrderMutationDto>> Handle(
        StartCheckoutCommand request, CancellationToken cancellationToken)
    {
        var order = await OrderRepository.FindAsync(db, request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotFound(request.OrderId));
        }

        if (order.Status != OrderStatus.Open)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotOpen);
        }

        order.StartCheckout();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await OrderResultFactory.BuildAsync(db, order, null, restaurant, cancellationToken));
    }
}
