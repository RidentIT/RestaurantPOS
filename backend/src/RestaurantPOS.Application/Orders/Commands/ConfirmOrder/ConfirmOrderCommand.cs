using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.ConfirmOrder;

/// <summary>
/// Saves the order and sends it to the kitchen (POS-008, POS-010, BR-POS-003, BR-POS-004). This
/// is where the order gets the number staff will call it by for the rest of the night (POS-009).
/// </summary>
public sealed record ConfirmOrderCommand(Guid OrderId) : IRequest<Result<OrderMutationDto>>;

public sealed class ConfirmOrderCommandValidator : AbstractValidator<ConfirmOrderCommand>
{
    public ConfirmOrderCommandValidator() => RuleFor(x => x.OrderId).NotEmpty();
}

internal sealed class ConfirmOrderCommandHandler(
    IAppDbContext db, IDateTimeProvider clock)
    : IRequestHandler<ConfirmOrderCommand, Result<OrderMutationDto>>
{
    public async Task<Result<OrderMutationDto>> Handle(
        ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await OrderRepository.FindAsync(db, request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotFound(request.OrderId));
        }

        if (order.Status != OrderStatus.Draft)
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NotDraft);
        }

        if (!order.ActiveItems.Any())
        {
            return Result.Failure<OrderMutationDto>(OrderErrors.NoItems);
        }

        var today = clock.Today;
        var orderNumber = await NextOrderNumberAsync(today, cancellationToken);

        var ticket = order.Confirm(orderNumber, today, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await OrderResultFactory.BuildAsync(db, order, ticket, cancellationToken));
    }

    /// <summary>
    /// The next number in today's sequence. Taken from the highest number already issued today
    /// rather than from a count, so cancelled orders keep their number and the sequence never
    /// hands the same one out twice (BR-POS-016).
    /// </summary>
    private async Task<int> NextOrderNumberAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var highest = await db.Orders
            .Where(o => o.OrderDate == today)
            .MaxAsync(o => (int?)o.OrderNumber, cancellationToken);

        return (highest ?? 0) + 1;
    }
}
