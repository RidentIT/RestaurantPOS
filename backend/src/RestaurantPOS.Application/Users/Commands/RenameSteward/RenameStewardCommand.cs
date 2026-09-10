using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Users.Commands.RenameSteward;

/// <summary>Corrects a steward's name. Orders already credited to them keep the new spelling.</summary>
public sealed record RenameStewardCommand(Guid StewardId, string Name) : IRequest<Result<StewardDto>>;

public sealed class RenameStewardCommandValidator : AbstractValidator<RenameStewardCommand>
{
    public RenameStewardCommandValidator()
    {
        RuleFor(x => x.StewardId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A steward's name is required.")
            .MaximumLength(Steward.NameMaxLength);
    }
}

internal sealed class RenameStewardCommandHandler(IAppDbContext db)
    : IRequestHandler<RenameStewardCommand, Result<StewardDto>>
{
    public async Task<Result<StewardDto>> Handle(RenameStewardCommand request, CancellationToken cancellationToken)
    {
        var steward = await db.Stewards.FirstOrDefaultAsync(s => s.Id == request.StewardId, cancellationToken);

        if (steward is null)
        {
            return Result.Failure<StewardDto>(StewardErrors.NotFound(request.StewardId));
        }

        var name = request.Name.Trim();
        var normalised = name.ToLowerInvariant();

        var taken = await db.Stewards
            .AnyAsync(s => s.Id != request.StewardId && s.Name.ToLower() == normalised, cancellationToken);

        if (taken)
        {
            return Result.Failure<StewardDto>(StewardErrors.NameTaken);
        }

        steward.Rename(name);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(steward.ToDto());
    }
}
