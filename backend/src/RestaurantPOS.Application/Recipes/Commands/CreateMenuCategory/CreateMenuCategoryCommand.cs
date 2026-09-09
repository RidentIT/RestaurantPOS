using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Commands.CreateMenuCategory;

/// <summary>
/// Registers a category name so it's offered when adding a menu item, whether or not a dish uses
/// it yet — the "Rice &amp; Curry" I want ready to pick from before the first curry is on the menu.
/// </summary>
public sealed record CreateMenuCategoryCommand(string Name) : IRequest<Result<string>>;

public sealed class CreateMenuCategoryCommandValidator : AbstractValidator<CreateMenuCategoryCommand>
{
    public CreateMenuCategoryCommandValidator() =>
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(MenuCategory.NameMaxLength);
}

internal sealed class CreateMenuCategoryCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateMenuCategoryCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var normalised = name.ToLowerInvariant();

        var alreadyRegistered = await db.MenuCategories.AnyAsync(c => c.Name.ToLower() == normalised, cancellationToken);
        var alreadyInUse = await db.MenuItems.AnyAsync(m => m.Category.ToLower() == normalised, cancellationToken);

        if (alreadyRegistered || alreadyInUse)
        {
            return Result.Failure<string>(RecipeErrors.CategoryNameTaken);
        }

        var category = MenuCategory.Create(name);
        db.MenuCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(category.Name);
    }
}
