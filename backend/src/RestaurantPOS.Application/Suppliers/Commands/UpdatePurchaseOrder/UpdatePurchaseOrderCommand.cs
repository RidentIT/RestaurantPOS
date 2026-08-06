using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Commands.UpdatePurchaseOrder;

public sealed record UpdatePurchaseOrderLineInput(Guid RawMaterialId, decimal Quantity, decimal UnitPrice);

/// <summary>Replaces a draft purchase order's lines and details. Only possible while still a Draft.</summary>
public sealed record UpdatePurchaseOrderCommand(
    Guid PurchaseOrderId,
    IReadOnlyCollection<UpdatePurchaseOrderLineInput> Lines,
    DateTime? ExpectedDeliveryDate,
    string? Notes) : IRequest<Result<PurchaseOrderDto>>;

public sealed class UpdatePurchaseOrderCommandValidator : AbstractValidator<UpdatePurchaseOrderCommand>
{
    public UpdatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative.");
        });

        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.RawMaterialId).Distinct().Count() == lines.Count)
            .WithMessage("A raw material cannot appear more than once on the same purchase order.")
            .When(x => x.Lines.Count > 0);

        RuleFor(x => x.Notes).MaximumLength(PurchaseOrder.NotesMaxLength);
    }
}

internal sealed class UpdatePurchaseOrderCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdatePurchaseOrderCommand, Result<PurchaseOrderDto>>
{
    public async Task<Result<PurchaseOrderDto>> Handle(
        UpdatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.PurchaseOrderNotFound(request.PurchaseOrderId));
        }

        if (order.Status != PurchaseOrderStatus.Draft)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.NotDraft);
        }

        var rawMaterialIds = request.Lines.Select(l => l.RawMaterialId).ToList();
        var rawMaterialCount = await db.RawMaterials.CountAsync(r => rawMaterialIds.Contains(r.Id), cancellationToken);
        if (rawMaterialCount != rawMaterialIds.Distinct().Count())
        {
            return Result.Failure<PurchaseOrderDto>(InventoryErrors.RawMaterialNotFound(rawMaterialIds[0]));
        }

        order.ReplaceLines(request.Lines.Select(l => (l.RawMaterialId, l.Quantity, l.UnitPrice)));
        order.UpdateDetails(request.ExpectedDeliveryDate, request.Notes);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await order.ToDtoAsync(db, cancellationToken));
    }
}