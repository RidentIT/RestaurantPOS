using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Commands.UpdateMenuItem;

/// <summary>
/// One size on the item after the edit — an existing size to update when <see cref="Id"/> is
/// supplied, or a new one to add when it is null. A size missing from the submitted list is removed.
/// </summary>
public sealed record MenuItemVariantInput(Guid? Id, string? Name, decimal Price);

public sealed record UpdateMenuItemCommand(
    Guid MenuItemId, string Name, string Category, IReadOnlyCollection<MenuItemVariantInput> Variants)
    : IRequest<Result<MenuItemDto>>;

public sealed class UpdateMenuItemCommandValidator : AbstractValidator<UpdateMenuItemCommand>
{
    public UpdateMenuItemCommandValidator()
    {
        RuleFor(x => x.MenuItemId).NotEmpty();

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

internal sealed class UpdateMenuItemCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateMenuItemCommand, Result<MenuItemDto>>
{
    public async Task<Result<MenuItemDto>> Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var menuItem = await db.MenuItems
            .Include(m => m.Variants)
            .FirstOrDefaultAsync(m => m.Id == request.MenuItemId, cancellationToken);

        if (menuItem is null)
        {
            return Result.Failure<MenuItemDto>(RecipeErrors.MenuItemNotFound(request.MenuItemId));
        }

        var name = request.Name.Trim();
        var normalised = name.ToLowerInvariant();

        var nameTaken = await db.MenuItems.AnyAsync(
            m => m.Id != request.MenuItemId && m.Name.ToLower() == normalised, cancellationToken);

        if (nameTaken)
        {
            return Result.Failure<MenuItemDto>(RecipeErrors.MenuItemNameTaken);
        }

        // The aggregate has no DB visibility, so removal-safety — a size that has already been
        // sold cannot just disappear — is enforced here, before ReplaceVariants ever runs.
        var keepIds = request.Variants.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet();
        var removedIds = menuItem.Variants.Select(v => v.Id).Where(id => !keepIds.Contains(id)).ToList();

        if (removedIds.Count > 0)
        {
            var soldRemovedCount = await db.OrderItems
                .Where(oi => removedIds.Contains(oi.MenuItemVariantId))
                .Select(oi => oi.MenuItemVariantId)
                .Distinct()
                .CountAsync(cancellationToken);

            if (soldRemovedCount > 0)
            {
                return Result.Failure<MenuItemDto>(RecipeErrors.VariantHasOrderHistory(soldRemovedCount));
            }
        }

        menuItem.UpdateDetails(name, request.Category);

        var variants = request.Variants
            .Select(v => new MenuItemVariantEdit(v.Id, v.Name, v.Price))
            .ToList();

        menuItem.ReplaceVariants(variants);

        await db.SaveChangesAsync(cancellationToken);

        var dto = await menuItem.ToDtoAsync(db, cancellationToken);

        return Result.Success(dto);
    }
}
