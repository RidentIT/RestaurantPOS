using System.Text.Json;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Audit;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Commands.DeleteRecipe;

/// <summary>
/// Removes a menu item's recipe. The audit log keeps a full snapshot of what was deleted
/// (BR-REC-007), even though the live row is gone — a fresh recipe can be created afterwards.
/// </summary>
public sealed record DeleteRecipeCommand(Guid MenuItemVariantId) : IRequest<Result>;

internal sealed class DeleteRecipeCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<DeleteRecipeCommand, Result>
{
    public async Task<Result> Handle(DeleteRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.MenuItemVariantId == request.MenuItemVariantId, cancellationToken);

        if (recipe is null)
        {
            return Result.Failure(RecipeErrors.RecipeNotFound(request.MenuItemVariantId));
        }

        var snapshot = recipe.Lines.Select(l => new { l.RawMaterialId, l.Quantity }).ToList();

        AuditLog.Record(
            db,
            entityType: "Recipe",
            entityId: recipe.Id,
            action: "Deleted",
            performedByUserId: currentUser.UserId!.Value,
            performedByName: currentUser.Username ?? "unknown",
            nowUtc: clock.UtcNow,
            summary: $"Deleted recipe that had {snapshot.Count} ingredient(s).",
            detailsJson: JsonSerializer.Serialize(snapshot));

        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}