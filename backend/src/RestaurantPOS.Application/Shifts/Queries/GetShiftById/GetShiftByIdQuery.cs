using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Shifts.Common;
using RestaurantPOS.Application.Shifts.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Shifts.Queries.GetShiftById;

/// <summary>
/// One shift with the orders rung up during it. A cashier may only open their own; the owner
/// (<paramref name="CanViewAll"/>) may open anyone's.
/// </summary>
public sealed record GetShiftByIdQuery(Guid Id, bool CanViewAll) : IRequest<Result<ShiftDetailDto>>;

internal sealed class GetShiftByIdQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetShiftByIdQuery, Result<ShiftDetailDto>>
{
    public async Task<Result<ShiftDetailDto>> Handle(GetShiftByIdQuery request, CancellationToken cancellationToken)
    {
        var shift = await db.CashierShifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (shift is null)
        {
            return Result.Failure<ShiftDetailDto>(ShiftErrors.NotFound(request.Id));
        }

        if (!request.CanViewAll && shift.CashierUserId != currentUser.UserId)
        {
            return Result.Failure<ShiftDetailDto>(ShiftErrors.NotYours);
        }

        var dto = (await ShiftReadModel.BuildAsync(db, [shift], cancellationToken)).Single();

        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.ShiftId == shift.Id)
            .ToListAsync(cancellationToken);

        var tableIds = orders.Where(o => o.TableId != null).Select(o => o.TableId!.Value).Distinct().ToList();
        var tableNumbers = await db.RestaurantTables.AsNoTracking()
            .Where(t => tableIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Number, cancellationToken);

        var rows = orders
            .OrderBy(o => o.ConfirmedAtUtc ?? o.CompletedAtUtc ?? DateTime.MaxValue)
            .Select(o => new ShiftOrderDto(
                o.Id,
                o.OrderNumber,
                o.TableId is { } t ? tableNumbers.GetValueOrDefault(t) : null,
                o.Status,
                o.Total,
                o.CompletedAtUtc))
            .ToList();

        return Result.Success(new ShiftDetailDto(dto, rows));
    }
}
