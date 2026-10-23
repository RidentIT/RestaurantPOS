using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Shifts.Dtos;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Shifts.Common;

/// <summary>
/// The arithmetic behind a shift: what it sold and what should be in its drawer. One place, so the
/// figure a cashier is shown at check-out is exactly the figure the owner reads afterwards.
/// </summary>
public static class ShiftReadModel
{
    private static readonly OrderStatus[] LiveStatuses = [OrderStatus.Draft, OrderStatus.Open, OrderStatus.Checkout];

    /// <summary>The cashier's open shift, or null when they haven't checked in.</summary>
    public static Task<CashierShift?> FindOpenShiftAsync(IAppDbContext db, Guid cashierUserId, CancellationToken ct) =>
        db.CashierShifts.FirstOrDefaultAsync(s => s.CashierUserId == cashierUserId && s.CheckedOutAtUtc == null, ct);

    /// <summary>Cash this shift's completed orders took: what closing the drawer should add up to.</summary>
    public static async Task<decimal> CashTakenAsync(IAppDbContext db, Guid shiftId, CancellationToken ct)
    {
        var amounts = await db.Orders.AsNoTracking()
            .Where(o => o.ShiftId == shiftId && o.Status == OrderStatus.Completed)
            .SelectMany(o => o.Payments)
            .Where(p => p.Method == OrderPaymentMethod.Cash)
            .Select(p => p.Amount)
            .ToListAsync(ct);

        return amounts.Sum();
    }

    public static Task<int> UnsettledOrderCountAsync(IAppDbContext db, Guid shiftId, CancellationToken ct) =>
        db.Orders.CountAsync(o => o.ShiftId == shiftId && LiveStatuses.Contains(o.Status), ct);

    /// <summary>Builds the full DTO, summary included, for each shift given.</summary>
    public static async Task<List<ShiftDto>> BuildAsync(
        IAppDbContext db, IReadOnlyCollection<CashierShift> shifts, CancellationToken ct)
    {
        if (shifts.Count == 0)
        {
            return [];
        }

        var shiftIds = shifts.Select(s => s.Id).ToList();
        var userIds = shifts.Select(s => s.CashierUserId).Distinct().ToList();

        var names = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Where(o => o.ShiftId != null && shiftIds.Contains(o.ShiftId.Value))
            .ToListAsync(ct);

        var byShift = orders.GroupBy(o => o.ShiftId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        return
        [
            .. shifts
                .OrderByDescending(s => s.CheckedInAtUtc)
                .Select(shift => ToDto(
                    shift,
                    names.GetValueOrDefault(shift.CashierUserId, "Unknown"),
                    Summarise(byShift.GetValueOrDefault(shift.Id) ?? []))),
        ];
    }

    public static ShiftSummaryDto Summarise(IReadOnlyCollection<Order> orders)
    {
        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

        var gross = completed.Sum(o => o.Subtotal);
        var discounts = completed.Sum(o => o.DiscountAmount);
        var net = gross - discounts;

        var payments = completed
            .SelectMany(o => o.Payments)
            .GroupBy(p => p.Method)
            .Select(g => new ShiftPaymentTotalDto(g.Key, g.Sum(p => p.Amount)))
            .OrderByDescending(p => p.Amount)
            .ToList();

        return new ShiftSummaryDto(
            completed.Count,
            completed.Sum(o => o.ActiveItems.Sum(i => i.Quantity)),
            Money(gross),
            Money(discounts),
            Money(net),
            completed.Count > 0 ? Money(net / completed.Count) : 0m,
            payments,
            payments.Where(p => p.Method == OrderPaymentMethod.Cash).Sum(p => p.Amount),
            orders.Count(o => o.IsLive));
    }

    public static ShiftDto ToDto(CashierShift shift, string cashierName, ShiftSummaryDto summary) =>
        new(
            shift.Id,
            shift.CashierUserId,
            cashierName,
            shift.CheckedInAtUtc,
            shift.CheckedOutAtUtc,
            shift.IsOpen,
            shift.OpeningCash,
            shift.ExpectedCash ?? shift.OpeningCash + summary.CashSales,
            shift.ClosingCash,
            shift.Variance,
            shift.Note,
            summary);

    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
