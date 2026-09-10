using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Users.Commands.CreateSteward;

/// <summary>Adds a steward to the roster so the till can credit a table to them.</summary>
public sealed record CreateStewardCommand(string Name) : IRequest<Result<StewardDto>>;

public sealed class CreateStewardCommandValidator : AbstractValidator<CreateStewardCommand>
{
    public CreateStewardCommandValidator() =>
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A steward's name is required.")
            .MaximumLength(Steward.NameMaxLength);
}

internal sealed class CreateStewardCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateStewardCommand, Result<StewardDto>>
{
    public async Task<Result<StewardDto>> Handle(CreateStewardCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var normalised = name.ToLowerInvariant();

        var taken = await db.Stewards.AnyAsync(s => s.Name.ToLower() == normalised, cancellationToken);

        if (taken)
        {
            return Result.Failure<StewardDto>(StewardErrors.NameTaken);
        }

        var steward = Steward.Create(name);
        db.Stewards.Add(steward);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(steward.ToDto());
    }
}
