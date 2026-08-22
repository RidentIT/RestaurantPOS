using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.CreateOrder;

/// <summary>
/// Opens a draft bill on a table (POS-001). The draft is persisted straight away rather than kept
/// at the till, so a cashier can walk between tables mid-order and nothing is lost if the machine
/// is restarted. It holds the table from this moment (POS-031).
/// </summary>
public sealed record CreateOrderCommand(Guid TableId) : IRequest<Result<OrderDto>>;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator() => RuleFor(x => x.TableId).NotEmpty();
}

internal sealed class CreateOrderCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private static readonly OrderStatus[] LiveStatuses =
        [OrderStatus.Draft, OrderStatus.Open, OrderStatus.Checkout];

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var table = await db.RestaurantTables
            .FirstOrDefaultAsync(t => t.Id == request.TableId, cancellationToken);

        if (table is null)
        {
            return Result.Failure<OrderDto>(OrderErrors.TableNotFound(request.TableId));
        }

        if (!table.IsActive)
        {
            return Result.Failure<OrderDto>(OrderErrors.TableInactive);
        }

        // One live order per table: two bills on the same table would each show a partial total
        // and the customer would be asked to pay twice for one sitting.
        var occupied = await db.Orders
            .AnyAsync(o => o.TableId == table.Id && LiveStatuses.Contains(o.Status), cancellationToken);

        if (occupied)
        {
            return Result.Failure<OrderDto>(OrderErrors.TableOccupied);
        }

        var settings = await RestaurantSettingsAccessor.GetAsync(db, cancellationToken);
        var order = Order.Create(
            table.Id, currentUser.UserId!.Value, settings.TaxRatePercent, settings.ServiceChargeRatePercent);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        var cashierName = await db.Users
            .Where(u => u.Id == order.CashierUserId)
            .Select(u => u.FullName)
            .FirstAsync(cancellationToken);

        var saved = await OrderRepository.FindAsync(db, order.Id, cancellationToken);

        return Result.Success(saved!.ToDto(table.Number, cashierName));
    }
}
