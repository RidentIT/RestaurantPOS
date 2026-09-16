using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Settings.Commands.RemoveFrequentMenuCategory;

/// <summary>
/// Unpins a category from the till's category strip. Removing one that isn't pinned is a silent
/// no-op, the mirror image of <c>AddFrequentMenuCategoryCommand</c>.
/// </summary>
public sealed record RemoveFrequentMenuCategoryCommand(string Category) : IRequest<Result<IReadOnlyList<string>>>;

public sealed class RemoveFrequentMenuCategoryCommandValidator : AbstractValidator<RemoveFrequentMenuCategoryCommand>
{
    public RemoveFrequentMenuCategoryCommandValidator() =>
        RuleFor(x => x.Category).NotEmpty().WithMessage("A category is required.");
}

internal sealed class RemoveFrequentMenuCategoryCommandHandler(IAppDbContext db)
    : IRequestHandler<RemoveFrequentMenuCategoryCommand, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(
        RemoveFrequentMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);

        settings.RemoveFrequentMenuCategory(request.Category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(settings.FrequentMenuCategories);
    }
}
