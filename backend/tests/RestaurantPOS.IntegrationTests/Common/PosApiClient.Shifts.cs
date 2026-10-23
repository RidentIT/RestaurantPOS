using System.Net.Http.Json;

namespace RestaurantPOS.IntegrationTests.Common;

public sealed record ShiftPaymentTotalResponse(string Method, decimal Amount);

public sealed record ShiftSummaryResponse(
    int OrdersCompleted,
    int ItemsSold,
    decimal GrossSales,
    decimal DiscountsGiven,
    decimal NetSales,
    decimal AverageBill,
    IReadOnlyCollection<ShiftPaymentTotalResponse> Payments,
    decimal CashSales,
    int UnsettledOrders);

public sealed record ShiftResponse(
    Guid Id,
    Guid CashierUserId,
    string CashierName,
    DateTime CheckedInAtUtc,
    DateTime? CheckedOutAtUtc,
    bool IsOpen,
    decimal OpeningCash,
    decimal ExpectedCash,
    decimal? ClosingCash,
    decimal? Variance,
    string? Note,
    ShiftSummaryResponse Summary);

public sealed record CurrentShiftResponse(ShiftResponse? Shift);

public sealed record ShiftOrderResponse(
    Guid OrderId, int? OrderNumber, string? TableNumber, string Status, decimal Total, DateTime? CompletedAtUtc);

public sealed record ShiftDetailResponse(ShiftResponse Shift, IReadOnlyCollection<ShiftOrderResponse> Orders);

public sealed partial class PosApiClient
{
    public Task<HttpResponseMessage> CheckInAsync(decimal openingCash) =>
        Http.PostAsJsonAsync($"{BaseUrl}/shifts/check-in", new { openingCash }, Json);

    public Task<HttpResponseMessage> CheckOutAsync(decimal closingCash, string? note = null) =>
        Http.PostAsJsonAsync($"{BaseUrl}/shifts/check-out", new { closingCash, note }, Json);

    public Task<HttpResponseMessage> GetCurrentShiftAsync() => Http.GetAsync($"{BaseUrl}/shifts/current");

    public Task<HttpResponseMessage> GetMyShiftsAsync() => Http.GetAsync($"{BaseUrl}/shifts/mine");

    public Task<HttpResponseMessage> GetShiftsAsync(Guid? cashierId = null) =>
        Http.GetAsync($"{BaseUrl}/shifts{(cashierId is null ? string.Empty : $"?cashierId={cashierId}")}");

    public Task<HttpResponseMessage> GetShiftAsync(Guid id) => Http.GetAsync($"{BaseUrl}/shifts/{id}");

    public Task<HttpResponseMessage> ForceCloseShiftAsync(Guid id, decimal? closingCash = null, string? note = null) =>
        Http.PostAsJsonAsync($"{BaseUrl}/shifts/{id}/force-close", new { closingCash, note }, Json);
}
