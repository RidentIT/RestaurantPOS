using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.SetTableActive;

/// <summary>Takes a table in or out of service, e.g. while it is being repaired.</summary>
public sealed record SetTableActiveCommand(Guid TableId, bool IsActive) : IRequest<Result<TableDto>>;

public sealed class SetTableActiveCommandValidator : AbstractValidator<SetTableActiveCommand>
{
    public SetTableActiveCommandValidator() => RuleFor(x => x.TableId).NotEmpty();
}

internal sealed class SetTableActiveCommandHandler(IAppDbContext db)
    : IRequestHandler<SetTableActiveCommand, Result<TableDto>>
{
    private static readonly OrderStatus[] LiveStatuses =
        [OrderStatus.Draft, OrderStatus.Open, OrderStatus.Checkout];

    public async Task<Result<TableDto>> Handle(SetTableActiveCommand request, CancellationToken cancellationToken)
    {
        var table = await db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == request.TableId, cancellationToken);

        if (table is null)
        {
            return Result.Failure<TableDto>(OrderErrors.TableNotFound(request.TableId));
        }

        // Pulling a table with customers sitting at it would strand their bill: the order could
        // no longer be reached from the floor plan, but it would still be holding the table.
        if (!request.IsActive)
        {
            var hasLiveOrder = await db.Orders
                .AnyAsync(o => o.TableId == table.Id && LiveStatuses.Contains(o.Status), cancellationToken);

            if (hasLiveOrder)
            {
                return Result.Failure<TableDto>(OrderErrors.TableInUse);
            }
        }

        if (request.IsActive)
        {
            table.Activate();
        }
        else
        {
            table.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new TableDto(
            table.Id, table.Number, table.Seats, table.Notes, table.IsActive, CurrentOrder: null));
    }
}
