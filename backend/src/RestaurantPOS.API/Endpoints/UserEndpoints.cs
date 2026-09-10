using MediatR;

using RestaurantPOS.API.Contracts.Users;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Users.Commands.CreateSteward;
using RestaurantPOS.Application.Users.Commands.CreateUser;
using RestaurantPOS.Application.Users.Commands.RenameSteward;
using RestaurantPOS.Application.Users.Commands.ResetUserPassword;
using RestaurantPOS.Application.Users.Commands.SetStewardActive;
using RestaurantPOS.Application.Users.Commands.SetUserActive;
using RestaurantPOS.Application.Users.Commands.UpdateUser;
using RestaurantPOS.Application.Users.Queries.GetModules;
using RestaurantPOS.Application.Users.Queries.GetStewards;
using RestaurantPOS.Application.Users.Queries.GetUserById;
using RestaurantPOS.Application.Users.Queries.GetUsers;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Endpoints;

/// <summary>Staff account administration. Every endpoint here requires the Admin role.</summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var group = routes.MapGroup("/users")
            .WithTags("Users")
            // Creating accounts and assigning modules is itself an administrative power, so it
            // is gated on the role rather than on a grantable module.
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        group.MapGet("/", async (
                string? search,
                UserRole? role,
                bool? isActive,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetUsersQuery(search, role, isActive), ct);
                return result.ToHttpResult();
            })
            .WithName("GetUsers")
            .WithSummary("Lists staff accounts, optionally filtered.");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetUserByIdQuery(id), ct);
                return result.ToHttpResult();
            })
            .WithName("GetUserById")
            .WithSummary("Loads a single staff account.");

        group.MapPost("/", async (CreateUserRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateUserCommand(
                    request.Username,
                    request.FullName,
                    request.Email,
                    request.Password,
                    request.Role,
                    request.Modules);

                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(user => $"/api/v1/users/{user.Id}");
            })
            .WithName("CreateUser")
            .WithSummary("Creates a staff account that must change its password at first sign-in.");

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateUserRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UpdateUserCommand(
                    id,
                    request.Username,
                    request.FullName,
                    request.Email,
                    request.Role,
                    request.Modules);

                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdateUser")
            .WithSummary("Updates a staff account's profile, role and module grants.");

        group.MapPut("/{id:guid}/status", async (
                Guid id,
                SetUserActiveRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new SetUserActiveCommand(id, request.IsActive), ct);
                return result.ToHttpResult();
            })
            .WithName("SetUserActive")
            .WithSummary("Activates or deactivates a staff account.");

        group.MapPost("/{id:guid}/password", async (
                Guid id,
                ResetUserPasswordRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new ResetUserPasswordCommand(id, request.NewPassword), ct);
                return result.ToHttpResult();
            })
            .WithName("ResetUserPassword")
            .WithSummary("Sets a temporary password the user must then change.");

        return routes;
    }

    /// <summary>
    /// The steward roster. Reading it is gated on POS &amp; Billing, since the order screen needs the
    /// picker; maintaining it is an administrative act, gated on the role like the rest of this file.
    /// </summary>
    public static IEndpointRouteBuilder MapStewardEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var group = routes.MapGroup("/stewards")
            .WithTags("Stewards")
            .RequireAuthorization();

        group.MapGet("/", async (bool? isActive, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStewardsQuery(isActive), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.PosBilling))
            .WithName("GetStewards")
            .WithSummary("Lists stewards; pass isActive=true for the order screen's picker.");

        group.MapPost("/", async (CreateStewardRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CreateStewardCommand(request.Name), ct);
                return result.ToCreatedResult(s => $"/api/v1/stewards/{s.Id}");
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("CreateSteward")
            .WithSummary("Adds a steward to the roster.");

        group.MapPut("/{id:guid}", async (
                Guid id, RenameStewardRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RenameStewardCommand(id, request.Name), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("RenameSteward")
            .WithSummary("Corrects a steward's name.");

        group.MapPut("/{id:guid}/status", async (
                Guid id, SetStewardActiveRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetStewardActiveCommand(id, request.IsActive), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("SetStewardActive")
            .WithSummary("Retires a steward who has left, or brings one back.");

        return routes;
    }

    /// <summary>
    /// Exposes the module catalog. Readable by any signed-in user because the client builds its
    /// navigation from it; the list itself is not sensitive.
    /// </summary>
    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapGet("/modules", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetModulesQuery(), ct);
                return result.ToHttpResult();
            })
            .WithTags("Modules")
            .RequireAuthorization()
            .WithName("GetModules")
            .WithSummary("Returns every module that can appear in navigation or be granted.");

        return routes;
    }
}