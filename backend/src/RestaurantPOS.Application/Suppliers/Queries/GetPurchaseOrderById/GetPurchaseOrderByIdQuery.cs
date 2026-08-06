using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Queries.GetPurchaseOrderById;

public sealed record GetPurchaseOrderByIdQuery(Guid PurchaseOrderId) : IRequest<Result<PurchaseOrderDto>>;

internal sealed class GetPurchaseOrderByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetPurchaseOrderByIdQuery, Result<PurchaseOrderDto>>
{
    public async Task<Result<PurchaseOrderDto>> Handle(
        GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders.AsNoTracking().Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.PurchaseOrderNotFound(request.PurchaseOrderId));
        }

        return Result.Success(await order.ToDtoAsync(db, cancellationToken));
    }
}