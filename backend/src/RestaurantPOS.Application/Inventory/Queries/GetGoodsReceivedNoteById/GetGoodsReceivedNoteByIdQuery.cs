using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Inventory.Queries.GetGoodsReceivedNoteById;

public sealed record GetGoodsReceivedNoteByIdQuery(Guid GoodsReceivedNoteId) : IRequest<Result<GoodsReceivedNoteDto>>;

internal sealed class GetGoodsReceivedNoteByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetGoodsReceivedNoteByIdQuery, Result<GoodsReceivedNoteDto>>
{
    public async Task<Result<GoodsReceivedNoteDto>> Handle(
        GetGoodsReceivedNoteByIdQuery request, CancellationToken cancellationToken)
    {
        var note = await db.GoodsReceivedNotes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == request.GoodsReceivedNoteId, cancellationToken);

        if (note is null)
        {
            return Result.Failure<GoodsReceivedNoteDto>(InventoryErrors.GoodsReceivedNoteNotFound(request.GoodsReceivedNoteId));
        }

        var supplier = await db.Suppliers.AsNoTracking().FirstAsync(s => s.Id == note.SupplierId, cancellationToken);
        var receivedByName = await db.Users.AsNoTracking().Where(u => u.Id == note.ReceivedByUserId)
            .Select(u => u.FullName).FirstAsync(cancellationToken);

        var movements = await db.StockMovements.AsNoTracking()
            .Where(m => m.ReferenceId == note.Id && m.Type == StockMovementType.GoodsReceived)
            .ToListAsync(cancellationToken);

        var rawMaterialIds = movements.Select(m => m.RawMaterialId).ToList();
        var rawMaterials = await db.RawMaterials.AsNoTracking()
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        var lines = movements
            .Select(m => new StockMovementLineDto(
                m.RawMaterialId, rawMaterials[m.RawMaterialId].Name, rawMaterials[m.RawMaterialId].UnitOfMeasurement, m.QuantityDelta))
            .OrderBy(l => l.RawMaterialName)
            .ToList();

        return Result.Success(new GoodsReceivedNoteDto(
            note.Id, note.SupplierId, supplier.Name, note.ReceivedByUserId, receivedByName,
            note.ReceivedAtUtc, note.Notes, note.PurchaseOrderId, note.QualityRating, note.HasIssue, lines));
    }
}