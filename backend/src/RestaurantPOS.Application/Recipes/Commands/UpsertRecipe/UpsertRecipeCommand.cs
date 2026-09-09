using System.Text.Json;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Audit;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Commands.UpsertRecipe;

/// <summary>One ingredient the caller wants on the recipe.</summary>
public sealed record RecipeLineInput(Guid RawMaterialId, decimal Quantity);

/// <summary>
/// Creates a menu item's recipe if it has none, or replaces its lines wholesale if it already
/// does (REC-006). One endpoint for both, since "one recipe per menu item" (BR-REC-001) makes
/// create-vs-update a distinction the caller should not need to track.
/// </summary>
public sealed record UpsertRecipeCommand(Guid MenuItemVariantId, IReadOnlyCollection<RecipeLineInput> Lines)
    : IRequest<Result<RecipeDto>>;

public sealed class UpsertRecipeCommandValidator : AbstractValidator<UpsertRecipeCommand>
{
    public UpsertRecipeCommandValidator()
    {
        RuleFor(x => x.MenuItemVariantId).NotEmpty();

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("A recipe must contain at least one raw material.");

        RuleForEach(x => x.Lines).ChildRules(line =>
            line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero."));

        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.RawMaterialId).Distinct().Count() == lines.Count)
            .WithMessage("A raw material cannot appear more than once in the same recipe.")
            .When(x => x.Lines.Count > 0);
    }
}

internal sealed class UpsertRecipeCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<UpsertRecipeCommand, Result<RecipeDto>>
{
    public async Task<Result<RecipeDto>> Handle(UpsertRecipeCommand request, CancellationToken cancellationToken)
    {
        var variantExists = await db.MenuItemVariants.AnyAsync(v => v.Id == request.MenuItemVariantId, cancellationToken);
        if (!variantExists)
        {
            return Result.Failure<RecipeDto>(RecipeErrors.VariantNotFound(request.MenuItemVariantId));
        }

        var rawMaterialIds = request.Lines.Select(l => l.RawMaterialId).ToList();

        var rawMaterials = await db.RawMaterials
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        if (rawMaterials.Count != rawMaterialIds.Distinct().Count())
        {
            return Result.Failure<RecipeDto>(RecipeErrors.UnknownRawMaterial);
        }

        if (rawMaterials.Values.Any(r => !r.IsActive))
        {
            return Result.Failure<RecipeDto>(RecipeErrors.InactiveRawMaterial);
        }

        var lines = request.Lines.Select(l => (l.RawMaterialId, l.Quantity)).ToList();

        var recipe = await db.Recipes
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.MenuItemVariantId == request.MenuItemVariantId, cancellationToken);
        var isNew = recipe is null;

        if (recipe is null)
        {
            recipe = Recipe.Create(request.MenuItemVariantId, lines);
            db.Recipes.Add(recipe);
        }
        else
        {
            recipe.ReplaceLines(lines);
        }

        var now = clock.UtcNow;

        AuditLog.Record(
            db,
            entityType: "Recipe",
            entityId: recipe.Id,
            action: isNew ? "Created" : "Updated",
            performedByUserId: currentUser.UserId!.Value,
            performedByName: currentUser.Username ?? "unknown",
            nowUtc: now,
            summary: $"{(isNew ? "Created" : "Updated")} recipe with {lines.Count} ingredient(s).",
            detailsJson: JsonSerializer.Serialize(lines));

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await recipe.ToDtoAsync(db, cancellationToken));
    }
}