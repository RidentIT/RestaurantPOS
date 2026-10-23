using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Shifts.Common;
using RestaurantPOS.Application.Shifts.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Shifts.Queries.GetMyCurrentShift;

/// <summary>The signed-in cashier's open shift with its running totals, or none if not checked in.</summary>
public sealed record GetMyCurrentShiftQuery : IRequest<Result<CurrentShiftDto>>;

internal sealed class GetMyCurrentShiftQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetMyCurrentShiftQuery, Result<CurrentShiftDto>>
{
    public async Task<Result<CurrentShiftDto>> Handle(GetMyCurrentShiftQuery request, CancellationToken cancellationToken)
    {
        var shift = await ShiftReadModel.FindOpenShiftAsync(db, currentUser.UserId!.Value, cancellationToken);

        if (shift is null)
        {
            return Result.Success(new CurrentShiftDto(null));
        }

        var dto = (await ShiftReadModel.BuildAsync(db, [shift], cancellationToken)).Single();

        return Result.Success(new CurrentShiftDto(dto));
    }
}
