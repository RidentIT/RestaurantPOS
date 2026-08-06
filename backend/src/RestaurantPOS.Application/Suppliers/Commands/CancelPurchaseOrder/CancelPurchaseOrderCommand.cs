using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Commands.CancelPurchaseOrder;

public sealed record CancelPurchaseOrderCommand(Guid PurchaseOrderId) : IRequest<Result<PurchaseOrderDto>>;

internal sealed class CancelPurchaseOrderCommandHandler(IAppDbContext db)
    : IRequestHandler<CancelPurchaseOrderCommand, Result<PurchaseOrderDto>>
{
    public async Task<Result<PurchaseOrderDto>> Handle(
        CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.PurchaseOrderNotFound(request.PurchaseOrderId));
        }

        if (order.Status is PurchaseOrderStatus.Delivered or PurchaseOrderStatus.Cancelled)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.NotCancellable);
        }

        order.Cancel();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await order.ToDtoAsync(db, cancellationToken));
    }
}