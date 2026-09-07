using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Reports.Common;
using RestaurantPOS.Application.Reports.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Reports.Queries.GetDailySalesReport;

/// <summary>One day's sales in depth: what sold, how it was paid for, and when the till was busy.</summary>
public sealed record GetDailySalesReportQuery(DateOnly Date) : IRequest<Result<SalesReportDto>>;

internal sealed class GetDailySalesReportQueryHandler(IAppDbContext db)
    : IRequestHandler<GetDailySalesReportQuery, Result<SalesReportDto>>
{
    public async Task<Result<SalesReportDto>> Handle(
        GetDailySalesReportQuery request, CancellationToken cancellationToken)
    {
        var report = await SalesAnalytics.BuildAsync(
            db, request.Date, request.Date, request.Date.ToString("yyyy-MM-dd"), cancellationToken);

        return Result.Success(report);
    }
}
