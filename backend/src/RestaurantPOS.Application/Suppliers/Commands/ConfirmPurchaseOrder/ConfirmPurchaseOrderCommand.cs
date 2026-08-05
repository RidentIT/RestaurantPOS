using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Commands.ConfirmPurchaseOrder;

/// <summary>Records that the supplier has agreed to fulfil the order as sent.</summary>
public sealed record ConfirmPurchaseOrderCommand(Guid PurchaseOrderId) : IRequest<Result<PurchaseOrderDto>>;

internal sealed class ConfirmPurchaseOrderCommandHandler(IAppDbContext db)
    : IRequestHandler<ConfirmPurchaseOrderCommand, Result<PurchaseOrderDto>>
{
    public async Task<Result<PurchaseOrderDto>> Handle(
        ConfirmPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.PurchaseOrderNotFound(request.PurchaseOrderId));
        }

        if (order.Status != PurchaseOrderStatus.Submitted)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.NotConfirmable);
        }

        order.Confirm();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await order.ToDtoAsync(db, cancellationToken));
    }
}