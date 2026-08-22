using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.ReprintReceipt;

/// <summary>
/// Prints a receipt again (POS-029). The reprint is counted so a bill that has been printed four
/// times is visible as such — the usual reason for asking is a customer wanting a second copy,
/// but a run of reprints on one order is worth a manager's attention.
/// </summary>
public sealed record ReprintReceiptCommand(Guid OrderId) : IRequest<Result<ReceiptDocumentDto>>;

public sealed class ReprintReceiptCommandValidator : AbstractValidator<ReprintReceiptCommand>
{
    public ReprintReceiptCommandValidator() => RuleFor(x => x.OrderId).NotEmpty();
}

internal sealed class ReprintReceiptCommandHandler(
    IAppDbContext db, IDateTimeProvider clock)
    : IRequestHandler<ReprintReceiptCommand, Result<ReceiptDocumentDto>>
{
    public async Task<Result<ReceiptDocumentDto>> Handle(
        ReprintReceiptCommand request, CancellationToken cancellationToken)
    {
        var order = await OrderRepository.FindAsync(db, request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<ReceiptDocumentDto>(OrderErrors.NotFound(request.OrderId));
        }

        if (order.Receipt is null)
        {
            return Result.Failure<ReceiptDocumentDto>(OrderErrors.NoReceipt);
        }

        order.Receipt.RecordReprint(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(
            await ReceiptDocumentFactory.BuildAsync(db, order, order.Receipt, cancellationToken));
    }
}
