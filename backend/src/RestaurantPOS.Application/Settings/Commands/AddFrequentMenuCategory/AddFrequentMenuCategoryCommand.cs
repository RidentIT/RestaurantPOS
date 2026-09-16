using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Settings.Commands.AddFrequentMenuCategory;

/// <summary>
/// Pins a category to the front of the till's category strip. Adding one already pinned is a
/// silent no-op rather than an error — a second cashier tapping the same shortcut a moment later
/// shouldn't see a failure for something that already happened.
/// </summary>
public sealed record AddFrequentMenuCategoryCommand(string Category) : IRequest<Result<IReadOnlyList<string>>>;

public sealed class AddFrequentMenuCategoryCommandValidator : AbstractValidator<AddFrequentMenuCategoryCommand>
{
    public AddFrequentMenuCategoryCommandValidator() =>
        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("A category is required.")
            .MaximumLength(RestaurantSettings.FrequentMenuCategoryMaxLength);
}

internal sealed class AddFrequentMenuCategoryCommandHandler(IAppDbContext db)
    : IRequestHandler<AddFrequentMenuCategoryCommand, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(
        AddFrequentMenuCategoryCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);

        settings.AddFrequentMenuCategory(request.Category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(settings.FrequentMenuCategories);
    }
}
