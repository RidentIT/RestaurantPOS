using FluentValidation;

using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Modules;

namespace RestaurantPOS.Application.Users.Common;

/// <summary>Validation shared by every command that assigns modules to a user.</summary>
internal static class ModuleRules
{
    /// <summary>
    /// Rejects unknown modules and administrative modules, which are reserved for the
    /// <see cref="UserRole.Admin"/> role rather than granted individually.
    /// </summary>
    public static IRuleBuilderOptions<T, IReadOnlyCollection<AppModule>?> MustBeAssignableModules<T>(
        this IRuleBuilder<T, IReadOnlyCollection<AppModule>?> ruleBuilder) =>
        ruleBuilder
            .Must(modules => modules is null || modules.All(ModuleCatalog.IsDefined))
            .WithMessage("One or more of the selected modules is not recognised.")
            .Must(modules => modules is null || modules.All(ModuleCatalog.IsAssignableToUser))
            .WithMessage(
                "Administrative modules cannot be granted individually. Give the user the Admin role instead.");
}