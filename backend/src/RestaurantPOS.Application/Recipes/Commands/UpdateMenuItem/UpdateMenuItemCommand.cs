using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Commands.UpdateMenuItem;

public sealed record UpdateMenuItemCommand(Guid MenuItemId, string Name, string Category, decimal Price)
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

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
    }
}

internal sealed class UpdateMenuItemCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateMenuItemCommand, Result<MenuItemDto>>
{
    public async Task<Result<MenuItemDto>> Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
    {
        var menuItem = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == request.MenuItemId, cancellationToken);

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

        menuItem.UpdateDetails(name, request.Category, request.Price);

        var hasRecipe = await db.Recipes.AnyAsync(r => r.MenuItemId == menuItem.Id, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new MenuItemDto(
            menuItem.Id, menuItem.Name, menuItem.Category, menuItem.Price, menuItem.IsActive,
            hasRecipe, menuItem.CreatedAtUtc));
    }
}