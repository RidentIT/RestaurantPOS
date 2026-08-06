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

namespace RestaurantPOS.Application.Inventory.Commands.CreateRawMaterial;

public sealed record CreateRawMaterialCommand(
    string Name,
    UnitOfMeasurement UnitOfMeasurement,
    decimal? MainStoreReorderLevel,
    decimal? KitchenParLevel) : IRequest<Result<RawMaterialDto>>;

public sealed class CreateRawMaterialCommandValidator : AbstractValidator<CreateRawMaterialCommand>
{
    public CreateRawMaterialCommandValidator()
    {
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

internal sealed class CreateRawMaterialCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateRawMaterialCommand, Result<RawMaterialDto>>
{
    public async Task<Result<RawMaterialDto>> Handle(CreateRawMaterialCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var normalised = name.ToLowerInvariant();

        var exists = await db.RawMaterials.AnyAsync(r => r.Name.ToLower() == normalised, cancellationToken);
        if (exists)
        {
            return Result.Failure<RawMaterialDto>(InventoryErrors.RawMaterialNameTaken);
        }

        var rawMaterial = RawMaterial.Create(
            name, request.UnitOfMeasurement, request.MainStoreReorderLevel, request.KitchenParLevel);

        db.RawMaterials.Add(rawMaterial);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(rawMaterial.ToDto());
    }
}