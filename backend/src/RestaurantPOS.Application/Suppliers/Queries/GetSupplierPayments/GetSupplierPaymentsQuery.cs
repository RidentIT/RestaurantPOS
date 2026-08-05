using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Suppliers.Queries.GetSupplierPayments;

/// <summary>Payments for one purchase order, or every payment for a supplier across all their orders.</summary>
public sealed record GetSupplierPaymentsQuery(Guid? PurchaseOrderId, Guid? SupplierId)
    : IRequest<Result<IReadOnlyCollection<SupplierPaymentDto>>>;

internal sealed class GetSupplierPaymentsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetSupplierPaymentsQuery, Result<IReadOnlyCollection<SupplierPaymentDto>>>
{
    public async Task<Result<IReadOnlyCollection<SupplierPaymentDto>>> Handle(
        GetSupplierPaymentsQuery request, CancellationToken cancellationToken)
    {
        var query = db.SupplierPayments.AsNoTracking().AsQueryable();

        if (request.PurchaseOrderId is not null)
        {
            query = query.Where(p => p.PurchaseOrderId == request.PurchaseOrderId.Value);
        }
        else if (request.SupplierId is not null)
        {
            var orderIds = await db.PurchaseOrders.AsNoTracking()
                .Where(o => o.SupplierId == request.SupplierId.Value)
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(p => orderIds.Contains(p.PurchaseOrderId));
        }

        var payments = await query.OrderByDescending(p => p.PaymentDateUtc).ToListAsync(cancellationToken);

        var userNames = await db.Users.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        IReadOnlyCollection<SupplierPaymentDto> result =
        [
            .. payments.Select(p => new SupplierPaymentDto(
                p.Id, p.PurchaseOrderId, p.Amount, p.PaymentDateUtc, p.Method, p.InvoiceReference,
                p.RecordedByUserId, userNames.GetValueOrDefault(p.RecordedByUserId, "(unknown)"), p.Notes)),
        ];

        return Result.Success(result);
    }
}