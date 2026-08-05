using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Common;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Inventory.Commands.CreateStockAdjustment;

/// <summary>
/// Corrects a raw material's balance in one store to match a physical count (INV-004). A reason
/// is mandatory — an adjustment with no explanation defeats the point of keeping a ledger.
/// </summary>
public sealed record CreateStockAdjustmentCommand(
    Guid RawMaterialId, StoreType Store, decimal QuantityDelta, string Reason)
    : IRequest<Result<StockMovementDto>>;

public sealed class CreateStockAdjustmentCommandValidator : AbstractValidator<CreateStockAdjustmentCommand>
{
    public CreateStockAdjustmentCommandValidator()
    {
        RuleFor(x => x.RawMaterialId).NotEmpty();

        RuleFor(x => x.Store).IsInEnum().WithMessage("Select a valid store.");

        RuleFor(x => x.QuantityDelta).NotEqual(0).WithMessage("The adjustment must change the balance.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required.")
            .MaximumLength(StockMovement.NotesMaxLength);
    }
}

internal sealed class CreateStockAdjustmentCommandHandler(
    IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<CreateStockAdjustmentCommand, Result<StockMovementDto>>
{
    public async Task<Result<StockMovementDto>> Handle(
        CreateStockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var rawMaterial = await db.RawMaterials.FirstOrDefaultAsync(r => r.Id == request.RawMaterialId, cancellationToken);
        if (rawMaterial is null)
        {
            return Result.Failure<StockMovementDto>(InventoryErrors.RawMaterialNotFound(request.RawMaterialId));
        }

        var now = clock.UtcNow;

        var movement = new StockMovementRequest(
            request.RawMaterialId, request.Store, request.QuantityDelta, StockMovementType.Adjustment,
            ReferenceId: null, request.Reason);

        var ledgerResult = await InventoryLedger.ApplyAsync(
            db, [movement], currentUser.UserId!.Value, now, cancellationToken);

        if (ledgerResult.IsFailure)
        {
            return Result.Failure<StockMovementDto>(ledgerResult.Error);
        }

        await db.SaveChangesAsync(cancellationToken);

        var recorded = await db.StockMovements.AsNoTracking()
            .Where(m => m.RawMaterialId == request.RawMaterialId && m.Type == StockMovementType.Adjustment)
            .OrderByDescending(m => m.OccurredAtUtc)
            .FirstAsync(cancellationToken);

        var performedByName = await db.Users.Where(u => u.Id == recorded.PerformedByUserId)
            .Select(u => u.FullName).FirstAsync(cancellationToken);

        return Result.Success(new StockMovementDto(
            recorded.Id, rawMaterial.Id, rawMaterial.Name, rawMaterial.UnitOfMeasurement, recorded.Store,
            recorded.QuantityDelta, recorded.Type, recorded.ReferenceId, recorded.PerformedByUserId,
            performedByName, recorded.OccurredAtUtc, recorded.Notes));
    }
}