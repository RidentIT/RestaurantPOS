using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Kitchen.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Kitchen.Commands.AdvanceKitchenTicket;

/// <summary>
/// Moves a ticket along the kitchen's queue — started, plated, carried out. Forward only: the
/// timestamps feed how long food is taking, so a ticket cannot be walked backwards.
/// </summary>
public sealed record AdvanceKitchenTicketCommand(Guid TicketId, KitchenTicketStatus Status)
    : IRequest<Result<KitchenTicketDto>>;

public sealed class AdvanceKitchenTicketCommandValidator : AbstractValidator<AdvanceKitchenTicketCommand>
{
    public AdvanceKitchenTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

internal sealed class AdvanceKitchenTicketCommandHandler(IAppDbContext db, IDateTimeProvider clock)
    : IRequestHandler<AdvanceKitchenTicketCommand, Result<KitchenTicketDto>>
{
    public async Task<Result<KitchenTicketDto>> Handle(
        AdvanceKitchenTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.KitchenTickets
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<KitchenTicketDto>(OrderErrors.TicketNotFound(request.TicketId));
        }

        if (request.Status <= ticket.Status)
        {
            return Result.Failure<KitchenTicketDto>(
                OrderErrors.TicketCannotGoBack(ticket.Status.ToString(), request.Status.ToString()));
        }

        ticket.Advance(request.Status, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        var order = await db.Orders.AsNoTracking()
            .Where(o => o.Id == ticket.OrderId)
            .Select(o => new { o.OrderNumber, o.TableId })
            .FirstAsync(cancellationToken);

        var tableNumber = await db.RestaurantTables.AsNoTracking()
            .Where(t => t.Id == order.TableId)
            .Select(t => t.Number)
            .FirstAsync(cancellationToken);

        return Result.Success(ticket.ToDto(order.OrderNumber, tableNumber, clock.UtcNow));
    }
}
