using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.AssignOrderSteward;

/// <summary>
/// Credits the order to the steward serving the table, or clears it when <see cref="StewardId"/>
/// is null. Allowed at any point before the bill is settled. A takeaway order has no steward.
/// </summary>
public sealed record AssignOrderStewardCommand(Guid OrderId, Guid? StewardId) : IRequest<Result<OrderDto>>;

public sealed class AssignOrderStewardCommandValidator : AbstractValidator<AssignOrderStewardCommand>
{
    public AssignOrderStewardCommandValidator() => RuleFor(x => x.OrderId).NotEmpty();
}

internal sealed class AssignOrderStewardCommandHandler(IAppDbContext db)
    : IRequestHandler<AssignOrderStewardCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(AssignOrderStewardCommand request, CancellationToken cancellationToken)
    {
        var order = await OrderRepository.WithAggregate(db.Orders)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderDto>(OrderErrors.NotFound(request.OrderId));
        }

        if (order.TableId is null && request.StewardId is not null)
        {
            return Result.Failure<OrderDto>(StewardErrors.NotOnTableOrder);
        }

        string? stewardName = null;

        if (request.StewardId is { } stewardId)
        {
            var steward = await db.Stewards.FirstOrDefaultAsync(s => s.Id == stewardId, cancellationToken);

            if (steward is null)
            {
                return Result.Failure<OrderDto>(StewardErrors.NotFound(stewardId));
            }

            if (!steward.IsActive)
            {
                return Result.Failure<OrderDto>(StewardErrors.Inactive);
            }

            stewardName = steward.Name;
        }

        try
        {
            order.AssignSteward(request.StewardId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDto>(Error.Conflict("Order.NotLive", ex.Message));
        }

        await db.SaveChangesAsync(cancellationToken);

        var tableNumber = await TableNumberResolver.ResolveAsync(db, order.TableId, cancellationToken);

        var cashierName = await db.Users
            .Where(u => u.Id == order.CashierUserId)
            .Select(u => u.FullName)
            .FirstAsync(cancellationToken);

        return Result.Success(order.ToDto(tableNumber, cashierName, stewardName));
    }
}
