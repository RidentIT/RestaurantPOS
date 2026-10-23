using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Shifts.Dtos;

/// <summary>What a shift took through one payment method.</summary>
public sealed record ShiftPaymentTotalDto(OrderPaymentMethod Method, decimal Amount);

/// <summary>A shift's sales, totalled from the orders rung up during it.</summary>
public sealed record ShiftSummaryDto(
    int OrdersCompleted,
    int ItemsSold,
    /// <summary>Menu value of everything sold, before any discount.</summary>
    decimal GrossSales,
    decimal DiscountsGiven,
    /// <summary>Gross less discounts.</summary>
    decimal NetSales,
    decimal AverageBill,
    IReadOnlyCollection<ShiftPaymentTotalDto> Payments,
    /// <summary>Cash the shift took — what it adds to the drawer.</summary>
    decimal CashSales,
    /// <summary>Orders opened in this shift still waiting to be paid or cancelled.</summary>
    int UnsettledOrders);

public sealed record ShiftDto(
    Guid Id,
    Guid CashierUserId,
    string CashierName,
    DateTime CheckedInAtUtc,
    DateTime? CheckedOutAtUtc,
    bool IsOpen,
    decimal OpeningCash,
    /// <summary>Opening cash plus cash sales — live while open, frozen once closed.</summary>
    decimal ExpectedCash,
    decimal? ClosingCash,
    /// <summary>Counted less expected: negative is a shortage.</summary>
    decimal? Variance,
    string? Note,
    ShiftSummaryDto Summary);

/// <summary>The signed-in cashier's open shift, or null when they haven't checked in.</summary>
public sealed record CurrentShiftDto(ShiftDto? Shift);

public sealed record ShiftOrderDto(
    Guid OrderId, int? OrderNumber, string? TableNumber, OrderStatus Status, decimal Total, DateTime? CompletedAtUtc);

public sealed record ShiftDetailDto(ShiftDto Shift, IReadOnlyCollection<ShiftOrderDto> Orders);
