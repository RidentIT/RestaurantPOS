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

namespace RestaurantPOS.Application.Inventory.Commands.CreateGoodsReceivedNote;

public sealed record GrnLineInput(Guid RawMaterialId, decimal Quantity);

/// <summary>
/// Records stock received from a supplier into the Main Store. There is no separate draft or
/// confirmation step (INV-002, INV-003) — a GRN is only ever entered once the goods have
/// actually been counted in, so recording it and confirming it are the same action.
/// </summary>
/// <param name="PurchaseOrderId">
/// Optional — reconciles this delivery against an order and marks it Delivered. A GRN can also
/// stand alone for a delivery that never had a formal PO raised against it.
/// </param>
/// <param name="QualityRating">Optional, 1 (worst) to 5 (best).</param>
/// <param name="HasIssue">Flags this delivery for the supplier performance view.</param>
public sealed record CreateGoodsReceivedNoteCommand(
    Guid SupplierId,
    IReadOnlyCollection<GrnLineInput> Lines,
    string? Notes,
    Guid? PurchaseOrderId = null,
    int? QualityRating = null,
    bool HasIssue = false) : IRequest<Result<GoodsReceivedNoteDto>>;

public sealed class CreateGoodsReceivedNoteCommandValidator : AbstractValidator<CreateGoodsReceivedNoteCommand>
{
    public CreateGoodsReceivedNoteCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one raw material line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
            line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero."));

        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.RawMaterialId).Distinct().Count() == lines.Count)
            .WithMessage("A raw material cannot appear more than once on the same GRN.")
            .When(x => x.Lines.Count > 0);

        RuleFor(x => x.Notes).MaximumLength(GoodsReceivedNote.NotesMaxLength);

        RuleFor(x => x.QualityRating)
            .InclusiveBetween(GoodsReceivedNote.MinQualityRating, GoodsReceivedNote.MaxQualityRating)
            .WithMessage($"Quality rating must be between {GoodsReceivedNote.MinQualityRating} and {GoodsReceivedNote.MaxQualityRating}.")
            .When(x => x.QualityRating is not null);
    }
}

internal sealed class CreateGoodsReceivedNoteCommandHandler(
    IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<CreateGoodsReceivedNoteCommand, Result<GoodsReceivedNoteDto>>
{
    public async Task<Result<GoodsReceivedNoteDto>> Handle(
        CreateGoodsReceivedNoteCommand request, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (supplier is null)
        {
            return Result.Failure<GoodsReceivedNoteDto>(SupplierErrors.NotFound(request.SupplierId));
        }

        if (!supplier.IsActive)
        {
            return Result.Failure<GoodsReceivedNoteDto>(SupplierErrors.Inactive);
        }

        PurchaseOrder? purchaseOrder = null;

        if (request.PurchaseOrderId is not null)
        {
            purchaseOrder = await db.PurchaseOrders
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId.Value, cancellationToken);

            if (purchaseOrder is null)
            {
                return Result.Failure<GoodsReceivedNoteDto>(SupplierErrors.PurchaseOrderNotFound(request.PurchaseOrderId.Value));
            }

            if (purchaseOrder.SupplierId != request.SupplierId)
            {
                return Result.Failure<GoodsReceivedNoteDto>(SupplierErrors.SupplierMismatch);
            }

            if (purchaseOrder.Status == PurchaseOrderStatus.Cancelled)
            {
                return Result.Failure<GoodsReceivedNoteDto>(SupplierErrors.PurchaseOrderCancelled);
            }
        }

        var rawMaterialIds = request.Lines.Select(l => l.RawMaterialId).ToList();
        var rawMaterials = await db.RawMaterials
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        if (rawMaterials.Count != rawMaterialIds.Distinct().Count())
        {
            return Result.Failure<GoodsReceivedNoteDto>(InventoryErrors.RawMaterialNotFound(rawMaterialIds.First(id => !rawMaterials.ContainsKey(id))));
        }

        if (rawMaterials.Values.Any(r => !r.IsActive))
        {
            return Result.Failure<GoodsReceivedNoteDto>(InventoryErrors.RawMaterialInactive);
        }

        var now = clock.UtcNow;
        var grn = GoodsReceivedNote.Create(
            request.SupplierId, currentUser.UserId!.Value, now, request.Notes,
            request.PurchaseOrderId, request.QualityRating, request.HasIssue);
        db.GoodsReceivedNotes.Add(grn);

        var movements = request.Lines
            .Select(l => new StockMovementRequest(
                l.RawMaterialId, StoreType.MainStore, l.Quantity, StockMovementType.GoodsReceived, grn.Id, null))
            .ToList();

        var ledgerResult = await InventoryLedger.ApplyAsync(db, movements, currentUser.UserId!.Value, now, cancellationToken);
        if (ledgerResult.IsFailure)
        {
            return Result.Failure<GoodsReceivedNoteDto>(ledgerResult.Error);
        }

        purchaseOrder?.MarkDelivered();

        await db.SaveChangesAsync(cancellationToken);

        var lines = request.Lines
            .Select(l => new StockMovementLineDto(l.RawMaterialId, rawMaterials[l.RawMaterialId].Name, rawMaterials[l.RawMaterialId].UnitOfMeasurement, l.Quantity))
            .OrderBy(l => l.RawMaterialName)
            .ToList();

        var receivedByName = await db.Users.Where(u => u.Id == grn.ReceivedByUserId)
            .Select(u => u.FullName).FirstAsync(cancellationToken);

        return Result.Success(new GoodsReceivedNoteDto(
            grn.Id, supplier.Id, supplier.Name, grn.ReceivedByUserId, receivedByName,
            grn.ReceivedAtUtc, grn.Notes, grn.PurchaseOrderId, grn.QualityRating, grn.HasIssue, lines));
    }
}