using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Inventory.Commands.UpdateRawMaterial;

public sealed record UpdateRawMaterialCommand(
    Guid RawMaterialId,
    string Name,
    UnitOfMeasurement UnitOfMeasurement,
    decimal? MainStoreReorderLevel,
    decimal? KitchenParLevel) : IRequest<Result<RawMaterialDto>>;

public sealed class UpdateRawMaterialCommandValidator : AbstractValidator<UpdateRawMaterialCommand>
{
    public UpdateRawMaterialCommandValidator()
    {
        RuleFor(x => x.RawMaterialId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(RawMaterial.NameMaxLength);

        RuleFor(x => x.UnitOfMeasurement).IsInEnum().WithMessage("Select a valid unit of measurement.");

        RuleFor(x => x.MainStoreReorderLevel).GreaterThanOrEqualTo(0)
            .WithMessage("Reorder level cannot be negative.")
            .When(x => x.MainStoreReorderLevel is not null);

        RuleFor(x => x.KitchenParLevel).GreaterThanOrEqualTo(0)
            .WithMessage("Par level cannot be negative.")
            .When(x => x.KitchenParLevel is not null);
    }
}

internal sealed class UpdateRawMaterialCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateRawMaterialCommand, Result<RawMaterialDto>>
{
    public async Task<Result<RawMaterialDto>> Handle(UpdateRawMaterialCommand request, CancellationToken cancellationToken)
    {
        var rawMaterial = await db.RawMaterials.FirstOrDefaultAsync(r => r.Id == request.RawMaterialId, cancellationToken);

        if (rawMaterial is null)
        {
            return Result.Failure<RawMaterialDto>(InventoryErrors.RawMaterialNotFound(request.RawMaterialId));
        }

        var name = request.Name.Trim();
        var normalised = name.ToLowerInvariant();

        var nameTaken = await db.RawMaterials.AnyAsync(
            r => r.Id != request.RawMaterialId && r.Name.ToLower() == normalised, cancellationToken);

        if (nameTaken)
        {
            return Result.Failure<RawMaterialDto>(InventoryErrors.RawMaterialNameTaken);
        }

        rawMaterial.UpdateDetails(name, request.UnitOfMeasurement, request.MainStoreReorderLevel, request.KitchenParLevel);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(rawMaterial.ToDto());
    }
}