using Microsoft.AspNetCore.Authorization;

using RestaurantPOS.Application.Common.Security;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Modules;

namespace RestaurantPOS.API.Security;

/// <summary>
/// Named authorization policies. One is registered per module so endpoints can simply say
/// which module they belong to.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Requires the Admin role.</summary>
    public const string AdminOnly = "role:admin";

    /// <summary>
    /// Reading the supplier list is needed by Main Store staff picking a supplier on a GRN, not
    /// just by Supplier Management itself — so it accepts either grant, unlike every other
    /// supplier endpoint (create/edit supplier, purchase orders, pricing, payments), which stay
    /// <see cref="AppModule.SupplierManagement"/>-only.
    /// </summary>
    public const string SupplierLookup = "suppliers:read";

    /// <summary>Builds the policy name guarding <paramref name="module"/>.</summary>
    public static string ForModule(AppModule module) => $"module:{module}";

    /// <summary>Registers the admin policy plus one policy per catalog module.</summary>
    public static AuthorizationBuilder AddAppPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy(AdminOnly, policy => policy.RequireRole(nameof(UserRole.Admin)));

        builder.AddPolicy(SupplierLookup, policy =>
            policy.RequireAssertion(context =>
                context.User.IsInRole(nameof(UserRole.Admin)) ||
                context.User.HasClaim(AppClaimTypes.Module, AppModule.SupplierManagement.ToString()) ||
                context.User.HasClaim(AppClaimTypes.Module, AppModule.StoreStockManagement.ToString())));

        foreach (var descriptor in ModuleCatalog.All)
        {
            var module = descriptor.Module;

            builder.AddPolicy(ForModule(module), policy =>
                policy.RequireAssertion(context =>
                    // Administrators hold every module implicitly, so their tokens carry no
                    // module claims; everyone else needs the specific grant.
                    context.User.IsInRole(nameof(UserRole.Admin)) ||
                    context.User.HasClaim(AppClaimTypes.Module, module.ToString())));
        }

        return builder;
    }
}