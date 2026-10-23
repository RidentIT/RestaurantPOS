using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Shifts.Common;
using RestaurantPOS.Application.Shifts.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Shifts.Queries.GetMyShifts;

/// <summary>
/// The signed-in cashier's own shifts from the last seven days, newest first. Older shifts are
/// kept on record and remain visible to the owner — seven days is only how far back a cashier
/// can look.
/// </summary>
public sealed record GetMyShiftsQuery : IRequest<Result<IReadOnlyCollection<ShiftDto>>>;

internal sealed class GetMyShiftsQueryHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<GetMyShiftsQuery, Result<IReadOnlyCollection<ShiftDto>>>
{
    private const int HistoryDays = 7;

    public async Task<Result<IReadOnlyCollection<ShiftDto>>> Handle(
        GetMyShiftsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var since = clock.UtcNow.AddDays(-HistoryDays);

        // A shift still open is always listed, however long ago it started.
        var shifts = await db.CashierShifts.AsNoTracking()
            .Where(s => s.CashierUserId == userId && (s.CheckedInAtUtc >= since || s.CheckedOutAtUtc == null))
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<ShiftDto> result = await ShiftReadModel.BuildAsync(db, shifts, cancellationToken);

        return Result.Success(result);
    }
}
