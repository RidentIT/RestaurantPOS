using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Kitchen.Commands.ReprintKitchenTicket;

/// <summary>
/// Prints a kitchen slip again — the printer jammed, or the docket went missing off the pass.
/// Reprints the ticket as it was originally sent, not as the order stands now, so the kitchen is
/// never handed a slip that quietly contradicts one they already have.
/// </summary>
public sealed record ReprintKitchenTicketCommand(Guid TicketId) : IRequest<Result<KotDocumentDto>>;

public sealed class ReprintKitchenTicketCommandValidator : AbstractValidator<ReprintKitchenTicketCommand>
{
    public ReprintKitchenTicketCommandValidator() => RuleFor(x => x.TicketId).NotEmpty();
}

internal sealed class ReprintKitchenTicketCommandHandler(IAppDbContext db, IRestaurantProfile restaurant)
    : IRequestHandler<ReprintKitchenTicketCommand, Result<KotDocumentDto>>
{
    public async Task<Result<KotDocumentDto>> Handle(
        ReprintKitchenTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.KitchenTickets
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<KotDocumentDto>(OrderErrors.TicketNotFound(request.TicketId));
        }

        var order = await OrderRepository.FindAsync(db, ticket.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<KotDocumentDto>(OrderErrors.NotFound(ticket.OrderId));
        }

        ticket.RecordReprint();
        await db.SaveChangesAsync(cancellationToken);

        var tableNumber = await db.RestaurantTables
            .Where(t => t.Id == order.TableId)
            .Select(t => t.Number)
            .FirstAsync(cancellationToken);

        var cashierName = await db.Users
            .Where(u => u.Id == order.CashierUserId)
            .Select(u => u.FullName)
            .FirstAsync(cancellationToken);

        return Result.Success(ticket.ToKotDocument(order, restaurant.Name, tableNumber, cashierName));
    }
}
