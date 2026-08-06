using MediatR;

using RestaurantPOS.API.Contracts.Kitchen;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Kitchen.Commands.AdvanceKitchenTicket;
using RestaurantPOS.Application.Kitchen.Commands.ReprintKitchenTicket;
using RestaurantPOS.Application.Kitchen.Queries.GetKitchenTickets;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Endpoints;

/// <summary>The kitchen display: the ticket queue and its preparation status.</summary>
public static class KitchenEndpoints
{
    public static IEndpointRouteBuilder MapKitchenEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var group = routes.MapGroup("/kitchen")
            .WithTags("Kitchen")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.KitchenOperations));

        group.MapGet("/tickets", async (bool? includeServed, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetKitchenTicketsQuery(includeServed ?? false), ct);
                return result.ToHttpResult();
            })
            .WithName("GetKitchenTickets")
            .WithSummary("Every slip still being worked, oldest first.");

        group.MapPut("/tickets/{id:guid}/status", async (
                Guid id, AdvanceKitchenTicketRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new AdvanceKitchenTicketCommand(id, request.Status), ct);
                return result.ToHttpResult();
            })
            .WithName("AdvanceKitchenTicket")
            .WithSummary("Moves a ticket to started, ready or served.");

        group.MapPost("/tickets/{id:guid}/reprint", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ReprintKitchenTicketCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("ReprintKitchenTicket")
            .WithSummary("Prints a kitchen slip again, exactly as it was first sent.");

        return routes;
    }
}
