using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Commands.SetSupplierPrice;

/// <summary>
/// Records the current price a supplier charges for a raw material. Every call appends a
/// history entry — prices are never overwritten in place — which is what lets a supplier's
/// price for an ingredient be tracked over time.
/// </summary>
public sealed record SetSupplierPriceCommand(Guid SupplierId, Guid RawMaterialId, decimal Price)
    : IRequest<Result<SupplierPriceDto>>;

public sealed class SetSupplierPriceCommandValidator : AbstractValidator<SetSupplierPriceCommand>
{
    public SetSupplierPriceCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.RawMaterialId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
    }
}

internal sealed class SetSupplierPriceCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<SetSupplierPriceCommand, Result<SupplierPriceDto>>
{
    public async Task<Result<SupplierPriceDto>> Handle(SetSupplierPriceCommand request, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (supplier is null)
        {
            return Result.Failure<SupplierPriceDto>(SupplierErrors.NotFound(request.SupplierId));
        }

        var rawMaterial = await db.RawMaterials.FirstOrDefaultAsync(r => r.Id == request.RawMaterialId, cancellationToken);
        if (rawMaterial is null)
        {
            return Result.Failure<SupplierPriceDto>(InventoryErrors.RawMaterialNotFound(request.RawMaterialId));
        }

        var now = clock.UtcNow;

        var current = await db.SupplierPrices.FirstOrDefaultAsync(
            p => p.SupplierId == request.SupplierId && p.RawMaterialId == request.RawMaterialId, cancellationToken);

        if (current is null)
        {
            current = new SupplierPrice(request.SupplierId, request.RawMaterialId, request.Price, now);
            db.SupplierPrices.Add(current);
        }
        else
        {
            current.UpdatePrice(request.Price, now);
        }

        db.SupplierPriceHistoryEntries.Add(SupplierPriceHistoryEntry.Record(
            request.SupplierId, request.RawMaterialId, request.Price, currentUser.UserId!.Value, now));

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new SupplierPriceDto(
            supplier.Id, supplier.Name, rawMaterial.Id, rawMaterial.Name, rawMaterial.UnitOfMeasurement,
            current.Price, current.UpdatedAtUtc));
    }
}