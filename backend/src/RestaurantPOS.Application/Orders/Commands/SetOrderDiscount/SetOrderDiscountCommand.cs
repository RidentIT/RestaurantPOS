using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.SetOrderDiscount;

/// <summary>Applies money off a bill, as a percentage or a flat amount (POS-007).</summary>
public sealed record SetOrderDiscountCommand(Guid OrderId, DiscountType Type, decimal Value)
    : IRequest<Result<OrderMutationDto>>;

public sealed class SetOrderDiscountCommandValidator : AbstractValidator<SetOrderDiscountCommand>
{
    public SetOrderDiscountCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0).WithMessage("A discount cannot be negative.");

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.Type == DiscountType.Percentage)
            .WithMessage("A percentage discount cannot exceed 100%.");
    }
}

internal sealed class SetOrderDiscountCommandHandler(IAppDbContext db)
    : IRequestHandler<SetOrderDiscountCommand, Result<OrderMutationDto>>
{
    public async Task<Result<OrderMutationDto>> Handle(
        SetOrderDiscountCommand request, CancellationToken cancellationToken)
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

        if (request.Type == DiscountType.Fixed && request.Value > order.Subtotal)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.DiscountExceedsSubtotal(order.Subtotal));
        }

        order.SetDiscount(request.Type, request.Value);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await OrderResultFactory.BuildAsync(db, order, null, cancellationToken));
    }
}
