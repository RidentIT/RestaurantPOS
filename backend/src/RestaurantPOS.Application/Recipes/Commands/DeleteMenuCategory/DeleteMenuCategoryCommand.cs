using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Commands.DeleteMenuCategory;

/// <summary>
/// Removes a category from the list offered when adding a menu item.
/// </summary>
/// <remarks>
/// Refused while any menu item still carries this category — deleting it out from under an item
/// wouldn't change that item's own <c>Category</c> string, it would just make the name harder to
/// find and easier to accidentally retype slightly differently next time, which is the exact
/// problem this whole list exists to prevent.
/// </remarks>
public sealed record DeleteMenuCategoryCommand(string Name) : IRequest<Result>;

public sealed class DeleteMenuCategoryCommandValidator : AbstractValidator<DeleteMenuCategoryCommand>
{
    public DeleteMenuCategoryCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().WithMessage("Category name is required.");
}

internal sealed class DeleteMenuCategoryCommandHandler(IAppDbContext db)
    : IRequestHandler<DeleteMenuCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        var normalised = request.Name.Trim().ToLowerInvariant();

        var itemsUsingIt = await db.MenuItems.CountAsync(m => m.Category.ToLower() == normalised, cancellationToken);
        if (itemsUsingIt > 0)
        {
            return Result.Failure(RecipeErrors.CategoryInUse(itemsUsingIt));
        }

        var category = await db.MenuCategories.FirstOrDefaultAsync(
            c => c.Name.ToLower() == normalised, cancellationToken);

        if (category is null)
        {
            return Result.Failure(RecipeErrors.CategoryNotFound(request.Name));
        }

        db.MenuCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
