using MediatR;

using RestaurantPOS.API.Contracts.Shifts;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Common.Security;
using RestaurantPOS.Application.Shifts.Commands.CheckIn;
using RestaurantPOS.Application.Shifts.Commands.CheckOut;
using RestaurantPOS.Application.Shifts.Commands.ForceCloseShift;
using RestaurantPOS.Application.Shifts.Queries.GetMyCurrentShift;
using RestaurantPOS.Application.Shifts.Queries.GetMyShifts;
using RestaurantPOS.Application.Shifts.Queries.GetShiftById;
using RestaurantPOS.Application.Shifts.Queries.GetShifts;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Endpoints;

/// <summary>A cashier's check-in / check-out, and the shift history built from it.</summary>
public static class ShiftEndpoints
{
    public static IEndpointRouteBuilder MapShiftEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var group = routes.MapGroup("/shifts").WithTags("Shifts");

        var pos = AuthorizationPolicies.ForModule(AppModule.PosBilling);

        group.MapPost("/check-in", async (CheckInRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CheckInCommand(request.OpeningCash), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(pos)
            .WithName("CheckIn")
            .WithSummary("Starts the signed-in cashier's shift with an opening cash float.");

        group.MapPost("/check-out", async (CheckOutRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CheckOutCommand(request.ClosingCash, request.Note), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(pos)
            .WithName("CheckOut")
            .WithSummary("Ends the signed-in cashier's shift, recording the cash they counted.");

        group.MapGet("/current", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyCurrentShiftQuery(), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(pos)
            .WithName("GetMyCurrentShift")
            .WithSummary("The signed-in cashier's open shift with running totals, or none.");

        group.MapGet("/mine", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyShiftsQuery(), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(pos)
            .WithName("GetMyShifts")
            .WithSummary("The signed-in cashier's own shifts from the last seven days.");

        group.MapGet("/", async (
                Guid? cashierId, DateOnly? from, DateOnly? to, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetShiftsQuery(cashierId, from, to), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.ShiftsReadAll)
            .WithName("GetShifts")
            .WithSummary("Every cashier's shifts, optionally filtered by cashier and date range.");

        group.MapGet("/{id:guid}", async (Guid id, HttpContext http, ISender sender, CancellationToken ct) =>
            {
                var user = http.User;
                var canViewAll = user.IsInRole(nameof(UserRole.Admin))
                    || user.HasClaim(AppClaimTypes.Module, AppModule.ReportsAnalytics.ToString());

                var result = await sender.Send(new GetShiftByIdQuery(id, canViewAll), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.ShiftsView)
            .WithName("GetShiftById")
            .WithSummary("One shift with its orders. A cashier can open only their own.");

        group.MapPost("/{id:guid}/force-close", async (
                Guid id, ForceCloseShiftRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ForceCloseShiftCommand(id, request.ClosingCash, request.Note), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.ShiftsReadAll)
            .WithName("ForceCloseShift")
            .WithSummary("Closes a shift on a cashier's behalf, e.g. one they forgot to check out of.");

        return routes;
    }
}
