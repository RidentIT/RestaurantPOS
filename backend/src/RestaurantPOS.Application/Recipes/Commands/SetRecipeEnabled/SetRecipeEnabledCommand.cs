using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Audit;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Recipes.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Recipes.Commands.SetRecipeEnabled;

/// <summary>Enables or disables a recipe (BR-REC-006). A disabled recipe cannot be used for a new sale.</summary>
public sealed record SetRecipeEnabledCommand(Guid MenuItemVariantId, bool IsEnabled) : IRequest<Result<RecipeDto>>;

internal sealed class SetRecipeEnabledCommandHandler(IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<SetRecipeEnabledCommand, Result<RecipeDto>>
{
    public async Task<Result<RecipeDto>> Handle(SetRecipeEnabledCommand request, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.MenuItemVariantId == request.MenuItemVariantId, cancellationToken);

        if (recipe is null)
        {
            return Result.Failure<RecipeDto>(RecipeErrors.RecipeNotFound(request.MenuItemVariantId));
        }

        if (request.IsEnabled)
        {
            recipe.Enable();
        }
        else
        {
            recipe.Disable();
        }

        AuditLog.Record(
            db,
            entityType: "Recipe",
            entityId: recipe.Id,
            action: request.IsEnabled ? "Enabled" : "Disabled",
            performedByUserId: currentUser.UserId!.Value,
            performedByName: currentUser.Username ?? "unknown",
            nowUtc: clock.UtcNow,
            summary: request.IsEnabled ? "Recipe enabled." : "Recipe disabled.");

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await recipe.ToDtoAsync(db, cancellationToken));
    }
}