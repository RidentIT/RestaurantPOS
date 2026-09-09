using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Commands.CreateMenuItem;

/// <summary>One size to create the new item with. Every item needs at least one.</summary>
public sealed record MenuItemVariantInput(string? Name, decimal Price);

public sealed record CreateMenuItemCommand(
    string Name, string Category, IReadOnlyCollection<MenuItemVariantInput> Variants)
    : IRequest<Result<MenuItemDto>>;

public sealed class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(MenuItem.NameMaxLength);

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(MenuItem.CategoryMaxLength);

        RuleFor(x => x.Variants)
            .NotEmpty().WithMessage("Add at least one size.");

        RuleForEach(x => x.Variants).ChildRules(variant =>
        {
            variant.RuleFor(v => v.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
            variant.RuleFor(v => v.Name).MaximumLength(MenuItemVariant.NameMaxLength);
        });

        RuleFor(x => x.Variants)
            .Must(v => v.Count == 1 || v.All(x => !string.IsNullOrWhiteSpace(x.Name)))
            .WithMessage("Every size needs its own name once there is more than one size.")
            .When(x => x.Variants.Count > 0);

        RuleFor(x => x.Variants)
            .Must(v => v.Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => x.Name!.Trim().ToLowerInvariant())
                .Distinct().Count() == v.Count(x => !string.IsNullOrWhiteSpace(x.Name)))
            .WithMessage("Two sizes on the same item cannot share a name.")
            .When(x => x.Variants.Count > 0);
    }
}

internal sealed class CreateMenuItemCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateMenuItemCommand, Result<MenuItemDto>>
{
    public async Task<Result<MenuItemDto>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var normalised = name.ToLowerInvariant();

        var exists = await db.MenuItems.AnyAsync(m => m.Name.ToLower() == normalised, cancellationToken);
        if (exists)
        {
            return Result.Failure<MenuItemDto>(RecipeErrors.MenuItemNameTaken);
        }

        var variants = request.Variants
            .Select(v => new MenuItemVariantEdit(Id: null, v.Name, v.Price))
            .ToList();

        var menuItem = MenuItem.Create(name, request.Category, variants);

        db.MenuItems.Add(menuItem);
        await db.SaveChangesAsync(cancellationToken);

        // Brand new — nothing on it has a recipe yet.
        return Result.Success(menuItem.ToDto(new HashSet<Guid>()));
    }
}
