using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Commands.SubmitPurchaseOrder;

/// <summary>Sends a draft purchase order to its supplier. No further line edits after this point.</summary>
public sealed record SubmitPurchaseOrderCommand(Guid PurchaseOrderId) : IRequest<Result<PurchaseOrderDto>>;

internal sealed class SubmitPurchaseOrderCommandHandler(IAppDbContext db, IDateTimeProvider clock)
    : IRequestHandler<SubmitPurchaseOrderCommand, Result<PurchaseOrderDto>>
{
    public async Task<Result<PurchaseOrderDto>> Handle(
        SubmitPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.PurchaseOrderNotFound(request.PurchaseOrderId));
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.NotSubmittable);
        }

        order.Submit(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await order.ToDtoAsync(db, cancellationToken));
    }
}