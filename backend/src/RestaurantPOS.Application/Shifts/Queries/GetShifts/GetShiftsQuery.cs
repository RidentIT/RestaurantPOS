using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Shifts.Common;
using RestaurantPOS.Application.Shifts.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Shifts.Queries.GetShifts;

/// <summary>
/// Every cashier's shifts, for the owner — optionally one cashier, optionally a date range
/// (the restaurant's local days, inclusive). Defaults to the last 30 days.
/// </summary>
public sealed record GetShiftsQuery(Guid? CashierUserId, DateOnly? From, DateOnly? To)
    : IRequest<Result<IReadOnlyCollection<ShiftDto>>>;

internal sealed class GetShiftsQueryHandler(IAppDbContext db, IDateTimeProvider clock)
    : IRequestHandler<GetShiftsQuery, Result<IReadOnlyCollection<ShiftDto>>>
{
    private const int MaxShifts = 300;

    public async Task<Result<IReadOnlyCollection<ShiftDto>>> Handle(
        GetShiftsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From ?? clock.Today.AddDays(-30);
        var to = request.To ?? clock.Today;

        var fromUtc = from.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
        var toUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue).ToUniversalTime();

        var query = db.CashierShifts.AsNoTracking()
            .Where(s => s.CheckedInAtUtc >= fromUtc && s.CheckedInAtUtc < toUtc);

        if (request.CashierUserId is { } cashierId)
        {
            query = query.Where(s => s.CashierUserId == cashierId);
        }

        var shifts = await query
            .OrderByDescending(s => s.CheckedInAtUtc)
            .Take(MaxShifts)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<ShiftDto> result = await ShiftReadModel.BuildAsync(db, shifts, cancellationToken);

        return Result.Success(result);
    }
}
