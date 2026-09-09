using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Orders.Common;

/// <summary>
/// Composes the printable receipt from a settled order.
/// </summary>
/// <remarks>
/// Rebuilt from the order every time rather than stored as rendered text, which is what makes a
/// reprint (POS-029) provably identical to the original: a completed order can no longer be
/// edited, so there is exactly one set of numbers it can ever produce.
/// </remarks>
public static class ReceiptDocumentFactory
{
    public static async Task<ReceiptDocumentDto> BuildAsync(
        IAppDbContext db,
        Order order,
        Receipt receipt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(receipt);

        var settings = await RestaurantSettingsAccessor.GetAsync(db, cancellationToken);

        var tableNumber = await TableNumberResolver.ResolveAsync(db, order.TableId, cancellationToken);

        var cashierName = await db.Users
            .Where(u => u.Id == order.CashierUserId)
            .Select(u => u.FullName)
            .FirstAsync(cancellationToken);

        return new ReceiptDocumentDto(
            receipt.Number,
            settings.Name,
            settings.AddressLine1,
            settings.AddressLine2,
            settings.City,
            settings.Phone,
            settings.VatRegistrationNumber,
            order.OrderNumber,
            tableNumber,
            cashierName,
            receipt.IssuedAtUtc,
            receipt.PrintCount,
            [.. order.ActiveItems
                .OrderBy(i => i.CreatedAtUtc)
                .Select(i => new ReceiptLineDto(i.MenuItemName, i.Quantity, i.UnitPrice, i.LineTotal))],
            order.Subtotal,
            order.DiscountAmount,
            order.ServiceChargeAmount,
            order.TaxAmount,
            order.Total,
            order.ChangeDue,
            [.. order.Payments.OrderBy(p => p.CreatedAtUtc).Select(p => p.ToDto())],
            receipt.Number,
            settings.ReceiptFooterMessage);
    }
}
