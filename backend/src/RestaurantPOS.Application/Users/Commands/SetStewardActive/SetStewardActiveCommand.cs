using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Users.Commands.SetStewardActive;

/// <summary>
/// Retires a steward who has left, or brings one back. A retired steward drops out of the order
/// picker but stays on past sales reports for the orders they served.
/// </summary>
public sealed record SetStewardActiveCommand(Guid StewardId, bool IsActive) : IRequest<Result<StewardDto>>;

public sealed class SetStewardActiveCommandValidator : AbstractValidator<SetStewardActiveCommand>
{
    public SetStewardActiveCommandValidator() => RuleFor(x => x.StewardId).NotEmpty();
}

internal sealed class SetStewardActiveCommandHandler(IAppDbContext db)
    : IRequestHandler<SetStewardActiveCommand, Result<StewardDto>>
{
    public async Task<Result<StewardDto>> Handle(SetStewardActiveCommand request, CancellationToken cancellationToken)
    {
        var steward = await db.Stewards.FirstOrDefaultAsync(s => s.Id == request.StewardId, cancellationToken);

        if (steward is null)
        {
            return Result.Failure<StewardDto>(StewardErrors.NotFound(request.StewardId));
        }

        if (request.IsActive)
        {
            steward.Activate();
        }
        else
        {
            steward.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(steward.ToDto());
    }
}
