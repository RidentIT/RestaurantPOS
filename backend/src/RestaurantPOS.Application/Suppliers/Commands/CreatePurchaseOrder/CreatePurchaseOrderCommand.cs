using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Commands.CreatePurchaseOrder;

public sealed record PurchaseOrderLineInput(Guid RawMaterialId, decimal Quantity, decimal UnitPrice);

/// <summary>Creates a new purchase order in Draft, ready to be edited further before being sent (INV/PO-001).</summary>
public sealed record CreatePurchaseOrderCommand(
    Guid SupplierId,
    IReadOnlyCollection<PurchaseOrderLineInput> Lines,
    DateTime? ExpectedDeliveryDate,
    string? Notes) : IRequest<Result<PurchaseOrderDto>>;

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();

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

internal sealed class CreatePurchaseOrderCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CreatePurchaseOrderCommand, Result<PurchaseOrderDto>>
{
    public async Task<Result<PurchaseOrderDto>> Handle(
        CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);
        if (supplier is null)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.NotFound(request.SupplierId));
        }

        if (!supplier.IsActive)
        {
            return Result.Failure<PurchaseOrderDto>(SupplierErrors.Inactive);
        }

        var rawMaterialIds = request.Lines.Select(l => l.RawMaterialId).ToList();
        var rawMaterialCount = await db.RawMaterials.CountAsync(r => rawMaterialIds.Contains(r.Id), cancellationToken);
        if (rawMaterialCount != rawMaterialIds.Distinct().Count())
        {
            return Result.Failure<PurchaseOrderDto>(InventoryErrors.RawMaterialNotFound(rawMaterialIds[0]));
        }

        var order = PurchaseOrder.Create(
            request.SupplierId,
            currentUser.UserId!.Value,
            request.Lines.Select(l => (l.RawMaterialId, l.Quantity, l.UnitPrice)),
            request.ExpectedDeliveryDate,
            request.Notes);

        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await order.ToDtoAsync(db, cancellationToken));
    }
}