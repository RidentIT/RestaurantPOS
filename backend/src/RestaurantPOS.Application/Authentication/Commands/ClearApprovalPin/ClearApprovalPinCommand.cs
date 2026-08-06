using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Authentication.Commands.ClearApprovalPin;

/// <summary>Removes the signed-in administrator's approval PIN.</summary>
public sealed record ClearApprovalPinCommand : IRequest<Result>;

internal sealed class ClearApprovalPinCommandHandler(
    IAppDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<ClearApprovalPinCommand, Result>
{
    public async Task<Result> Handle(ClearApprovalPinCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return Result.Failure(AuthErrors.InvalidCredentials);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidCredentials);
        }

        if (!user.HasApprovalPin)
        {
            return Result.Failure(AuthErrors.PinNotSet);
        }

        user.ClearApprovalPin();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}