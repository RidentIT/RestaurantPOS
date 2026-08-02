using Microsoft.AspNetCore.Mvc;

using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Common.Security;

namespace RestaurantPOS.API.Middleware;

/// <summary>
/// Confines a user who has not yet chosen their own password to the password-change flow.
/// </summary>
/// <remarks>
/// The frontend also routes such users straight to the reset screen, but enforcing it here as
/// well means a temporary password issued by an administrator cannot be used to do real work
/// by calling the API directly.
/// </remarks>
public sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    /// <summary>Machine-readable code the client keys on to show the reset screen.</summary>
    public const string ErrorCode = "Auth.PasswordChangeRequired";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var mustChange = context.User.HasClaim(AppClaimTypes.MustChangePassword, "true");

        var endpointIsExempt = context.GetEndpoint()?.Metadata
            .GetMetadata<AllowPendingPasswordChangeAttribute>() is not null;

        if (!mustChange || endpointIsExempt)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "You must choose a new password before continuing.",
            Status = StatusCodes.Status403Forbidden,
            Extensions = { ["code"] = ErrorCode },
        });
    }
}