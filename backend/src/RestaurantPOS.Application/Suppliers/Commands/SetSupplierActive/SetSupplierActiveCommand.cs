using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Commands.SetSupplierActive;

public sealed record SetSupplierActiveCommand(Guid SupplierId, bool IsActive) : IRequest<Result<SupplierDto>>;

internal sealed class SetSupplierActiveCommandHandler(IAppDbContext db)
    : IRequestHandler<SetSupplierActiveCommand, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(SetSupplierActiveCommand request, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);

        if (supplier is null)
        {
            return Result.Failure<SupplierDto>(SupplierErrors.NotFound(request.SupplierId));
        }

        if (request.IsActive)
        {
            supplier.Activate();
        }
        else
        {
            supplier.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(supplier.ToDto());
    }
}