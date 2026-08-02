using MediatR;

using RestaurantPOS.API.Contracts.Auth;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Authentication.Commands.ChangePassword;
using RestaurantPOS.Application.Authentication.Commands.ClearApprovalPin;
using RestaurantPOS.Application.Authentication.Commands.Login;
using RestaurantPOS.Application.Authentication.Commands.Logout;
using RestaurantPOS.Application.Authentication.Commands.RefreshSession;
using RestaurantPOS.Application.Authentication.Commands.SetApprovalPin;
using RestaurantPOS.Application.Authentication.Commands.VerifyApprovalPin;
using RestaurantPOS.Application.Authentication.Queries.GetCurrentUser;

namespace RestaurantPOS.API.Endpoints;

/// <summary>Sign-in, session lifecycle and the administrator approval PIN.</summary>
public static class AuthEndpoints
{
    /// <summary>Rate-limiter policy guarding endpoints that accept a guessable secret.</summary>
    public const string SensitiveRateLimitPolicy = "sensitive";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var group = routes.MapGroup("/auth").WithTags("Authentication");

        group.MapPost("/login", async (LoginRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new LoginCommand(request.Username, request.Password), ct);
                return result.ToHttpResult();
            })
            .AllowAnonymous()
            .RequireRateLimiting(SensitiveRateLimitPolicy)
            .WithName("Login")
            .WithSummary("Signs in with a username and password.");

        group.MapPost("/refresh", async (RefreshRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RefreshSessionCommand(request.RefreshToken), ct);
                return result.ToHttpResult();
            })
            .AllowAnonymous()
            .WithName("RefreshSession")
            .WithSummary("Exchanges a refresh token for a new session.");

        group.MapPost("/logout", async (LogoutRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new LogoutCommand(request.RefreshToken), ct);
                return result.ToHttpResult();
            })
            .AllowAnonymous()
            .WithName("Logout")
            .WithSummary("Revokes a refresh token.");

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetCurrentUserQuery(), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            // Reachable mid-reset so the client can render the user's name on that screen.
            .WithMetadata(new AllowPendingPasswordChangeAttribute())
            .WithName("GetCurrentUser")
            .WithSummary("Returns the signed-in user and their effective module access.");

        group.MapPost("/change-password",
                async (ChangePasswordRequest request, ISender sender, CancellationToken ct) =>
                {
                    var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
                    var result = await sender.Send(command, ct);
                    return result.ToHttpResult();
                })
            .RequireAuthorization()
            // The whole point of this endpoint is to clear the pending-change state.
            .WithMetadata(new AllowPendingPasswordChangeAttribute())
            .RequireRateLimiting(SensitiveRateLimitPolicy)
            .WithName("ChangePassword")
            .WithSummary("Changes the signed-in user's password and returns a fresh session.");

        MapApprovalPinEndpoints(group);

        return routes;
    }

    private static void MapApprovalPinEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/pin", async (SetApprovalPinRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new SetApprovalPinCommand(request.CurrentPassword, request.Pin);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .RequireRateLimiting(SensitiveRateLimitPolicy)
            .WithName("SetApprovalPin")
            .WithSummary("Sets or generates the administrator's 4-digit approval PIN.");

        group.MapDelete("/pin", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ClearApprovalPinCommand(), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("ClearApprovalPin")
            .WithSummary("Removes the administrator's approval PIN.");

        group.MapPost("/pin/verify",
                async (VerifyApprovalPinRequest request, ISender sender, CancellationToken ct) =>
                {
                    var command = new VerifyApprovalPinCommand(request.Pin, request.Reason);
                    var result = await sender.Send(command, ct);
                    return result.ToHttpResult();
                })
            // Any signed-in user may present a PIN: the point is that a cashier calls this with
            // an administrator standing over their shoulder to authorise, say, a void.
            .RequireAuthorization()
            .RequireRateLimiting(SensitiveRateLimitPolicy)
            .WithName("VerifyApprovalPin")
            .WithSummary("Authorises a privileged action with an administrator's PIN.");
    }
}