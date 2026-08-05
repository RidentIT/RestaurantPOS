using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Inventory.Commands.SetRawMaterialActive;

public sealed record SetRawMaterialActiveCommand(Guid RawMaterialId, bool IsActive) : IRequest<Result<RawMaterialDto>>;

internal sealed class SetRawMaterialActiveCommandHandler(IAppDbContext db)
    : IRequestHandler<SetRawMaterialActiveCommand, Result<RawMaterialDto>>
{
    public async Task<Result<RawMaterialDto>> Handle(SetRawMaterialActiveCommand request, CancellationToken cancellationToken)
    {
        var rawMaterial = await db.RawMaterials.FirstOrDefaultAsync(r => r.Id == request.RawMaterialId, cancellationToken);

        if (rawMaterial is null)
        {
            return Result.Failure<RawMaterialDto>(InventoryErrors.RawMaterialNotFound(request.RawMaterialId));
        }

        if (request.IsActive)
        {
            rawMaterial.Activate();
        }
        else
        {
            rawMaterial.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(rawMaterial.ToDto());
    }
}