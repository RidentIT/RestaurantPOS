using MediatR;

using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Reports.Queries.GetDailySalesReport;
using RestaurantPOS.Application.Reports.Queries.GetMonthlySalesReport;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Endpoints;

/// <summary>
/// Sales analytics: best sellers, category and payment mix, and peak hours. The revenue-vs-expense
/// reports these sit alongside live under <c>/expenses/reports</c> — see
/// <see cref="AuthorizationPolicies.ReportsRead"/> for why they're reachable from here too.
/// </summary>
public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var group = routes.MapGroup("/reports")
            .WithTags("Reports")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.ReportsAnalytics));

        group.MapGet("/sales/daily", async (DateOnly date, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDailySalesReportQuery(date), ct);
                return result.ToHttpResult();
            })
            .WithName("GetDailySalesReport")
            .WithSummary("A day's sales in depth: best sellers, payment mix and peak hours.");

        group.MapGet("/sales/monthly", async (int year, int month, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMonthlySalesReportQuery(year, month), ct);
                return result.ToHttpResult();
            })
            .WithName("GetMonthlySalesReport")
            .WithSummary("A month's sales in depth: best sellers, category mix, payment mix and peak hours.");

        return routes;
    }
}
