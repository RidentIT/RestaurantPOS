using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Commands.CreateMenuItem;

public sealed record CreateMenuItemCommand(string Name, string Category, decimal Price)
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

        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
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

        var menuItem = MenuItem.Create(name, request.Category, request.Price);

        db.MenuItems.Add(menuItem);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new MenuItemDto(
            menuItem.Id, menuItem.Name, menuItem.Category, menuItem.Price, menuItem.IsActive,
            HasRecipe: false, menuItem.CreatedAtUtc));
    }
}