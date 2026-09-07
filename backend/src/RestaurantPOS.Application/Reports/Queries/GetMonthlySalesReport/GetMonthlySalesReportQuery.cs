using System.Globalization;

using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Reports.Common;
using RestaurantPOS.Application.Reports.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Reports.Queries.GetMonthlySalesReport;

/// <summary>A month's sales in depth: best sellers, category mix, payment mix, and peak hours.</summary>
public sealed record GetMonthlySalesReportQuery(int Year, int Month) : IRequest<Result<SalesReportDto>>;

public sealed class GetMonthlySalesReportQueryValidator : AbstractValidator<GetMonthlySalesReportQuery>
{
    public GetMonthlySalesReportQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2200);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

internal sealed class GetMonthlySalesReportQueryHandler(IAppDbContext db)
    : IRequestHandler<GetMonthlySalesReportQuery, Result<SalesReportDto>>
{
    public async Task<Result<SalesReportDto>> Handle(
        GetMonthlySalesReportQuery request, CancellationToken cancellationToken)
    {
        var from = new DateOnly(request.Year, request.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var report = await SalesAnalytics.BuildAsync(
            db, from, to, from.ToString("MMMM yyyy", CultureInfo.InvariantCulture), cancellationToken);

        return Result.Success(report);
    }
}
