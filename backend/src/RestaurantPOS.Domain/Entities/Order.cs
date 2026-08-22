using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>A dish being added to a bill, priced from the menu at the moment it is ordered.</summary>
public sealed record NewOrderItem(
    Guid MenuItemId, string MenuItemName, decimal UnitPrice, int Quantity, string? SpecialInstructions);

/// <summary>
/// A table's bill, from the first dish keyed in to the receipt handed over.
/// </summary>
/// <remarks>
/// The aggregate owns kitchen ticket creation rather than leaving it to the caller, because "the
/// kitchen is told whenever a confirmed order changes" (BR-POS-004, BR-POS-006, POS-020) is the
/// rule that stops the pass cooking one thing while the bill says another. Every mutator that can
/// change what the kitchen should be cooking returns the ticket it raised — or null while the
/// order is still a <see cref="OrderStatus.Draft"/> and nothing has been sent — so a caller
/// cannot change the order without being handed the slip that has to go out.
/// </remarks>
public sealed class Order : BaseEntity
{
    private readonly List<OrderItem> _items = [];
    private readonly List<KitchenTicket> _tickets = [];
    private readonly List<OrderPayment> _payments = [];

    // EF Core materialisation.
    private Order()
    {
    }

    private Order(Guid tableId, Guid cashierUserId, decimal taxRatePercent, decimal serviceChargeRatePercent)
    {
        TableId = tableId;
        CashierUserId = cashierUserId;
        Status = OrderStatus.Draft;
        DiscountType = DiscountType.None;
        TaxRatePercent = taxRatePercent;
        ServiceChargeRatePercent = serviceChargeRatePercent;
    }

    /// <summary>Sequence within <see cref="OrderDate"/>, assigned on confirmation (POS-009).</summary>
    public int? OrderNumber { get; private set; }

    /// <summary>Business day the number belongs to; the sequence restarts each day.</summary>
    public DateOnly? OrderDate { get; private set; }

    public Guid TableId { get; private set; }

    /// <summary>Who keyed the order in (POS-012).</summary>
    public Guid CashierUserId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DiscountType DiscountType { get; private set; }

    /// <summary>A percentage when <see cref="DiscountType"/> is Percentage, otherwise an amount.</summary>
    public decimal DiscountValue { get; private set; }

    /// <summary>
    /// The restaurant's tax rate at the moment this order was created, 0-100. Captured once rather
    /// than read live, so an administrator changing the rate mid-service does not retroactively
    /// change the total of a bill a customer is already looking at — the same reasoning that fixes
    /// a menu item's price onto <see cref="OrderItem"/> when it is added.
    /// </summary>
    public decimal TaxRatePercent { get; private set; }

    /// <summary>The restaurant's service charge rate at the moment this order was created, 0-100.</summary>
    public decimal ServiceChargeRatePercent { get; private set; }

    public DateTime? ConfirmedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    /// <summary>The administrator whose PIN authorised cancelling the whole order (BR-POS-009).</summary>
    public Guid? CancelledByUserId { get; private set; }

    public Receipt? Receipt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public IReadOnlyCollection<KitchenTicket> Tickets => _tickets.AsReadOnly();

    public IReadOnlyCollection<OrderPayment> Payments => _payments.AsReadOnly();

    /// <summary>Lines still on the bill — everything not voided.</summary>
    public IEnumerable<OrderItem> ActiveItems => _items.Where(i => !i.IsCancelled);

    public decimal Subtotal => ActiveItems.Sum(i => i.LineTotal);

    /// <summary>
    /// Money off the bill. Always recomputed from the subtotal and capped by it, so a fixed
    /// discount set against a larger bill can never exceed a bill that has since shrunk
    /// (BR-POS-010).
    /// </summary>
    public decimal DiscountAmount => DiscountType switch
    {
        DiscountType.Percentage => Math.Round(Subtotal * DiscountValue / 100m, 2, MidpointRounding.AwayFromZero),
        DiscountType.Fixed => Math.Min(DiscountValue, Subtotal),
        _ => 0m,
    };

    /// <summary>
    /// Service charge on top of the discounted subtotal (BR-POS-012). Zero unless the restaurant
    /// has set a rate — by default nothing is added, matching the original fixed rule this
    /// replaced.
    /// </summary>
    public decimal ServiceChargeAmount =>
        Math.Round((Subtotal - DiscountAmount) * ServiceChargeRatePercent / 100m, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Tax on top of the discounted subtotal and the service charge (BR-POS-011) — VAT is charged
    /// on the service charge too, which is the usual local convention. Zero unless the restaurant
    /// has set a rate.
    /// </summary>
    public decimal TaxAmount =>
        Math.Round(
            (Subtotal - DiscountAmount + ServiceChargeAmount) * TaxRatePercent / 100m,
            2,
            MidpointRounding.AwayFromZero);

    /// <summary>What the customer owes.</summary>
    public decimal Total => Subtotal - DiscountAmount + ServiceChargeAmount + TaxAmount;

    public decimal AmountPaid => _payments.Sum(p => p.Amount);

    /// <summary>Cash to hand back across all tenders (POS-024).</summary>
    public decimal ChangeDue => _payments.Sum(p => p.ChangeGiven);

    /// <summary>True while the order still holds its table (BR-POS-017).</summary>
    public bool IsLive => Status is OrderStatus.Draft or OrderStatus.Open or OrderStatus.Checkout;

    public static Order Create(
        Guid tableId, Guid cashierUserId, decimal taxRatePercent = 0m, decimal serviceChargeRatePercent = 0m) =>
        new(tableId, cashierUserId, taxRatePercent, serviceChargeRatePercent);

    /// <summary>
    /// Adds dishes to the bill. Free at any time on an open order and never needs approval
    /// (BR-POS-005); prints an addition ticket once the kitchen already has the order.
    /// </summary>
    /// <returns>The ticket raised, or null while the order is still a draft.</returns>
    public KitchenTicket? AddItems(IEnumerable<NewOrderItem> items, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(items);
        EnsureEditable();

        var added = items
            .Select(i => new OrderItem(Id, i.MenuItemId, i.MenuItemName, i.UnitPrice, i.Quantity, i.SpecialInstructions))
            .ToList();

        if (added.Count == 0)
        {
            throw new ArgumentException("At least one item is required.", nameof(items));
        }

        _items.AddRange(added);

        if (Status != OrderStatus.Open)
        {
            return null;
        }

        var ticket = CreateTicket(KitchenTicketKind.Addition, nowUtc);
        foreach (var item in added)
        {
            ticket.AddLine(item.Id, item.MenuItemName, item.Quantity, item.SpecialInstructions, null);
        }

        return ticket;
    }

    /// <summary>
    /// Saves the order and sends it to the kitchen (BR-POS-003, BR-POS-004). The order number is
    /// supplied by the caller, which is the only place that can see the rest of the day's orders.
    /// </summary>
    public KitchenTicket Confirm(int orderNumber, DateOnly orderDate, DateTime nowUtc)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft order can be confirmed.");
        }

        if (!ActiveItems.Any())
        {
            throw new InvalidOperationException("An order must have at least one item before it can be confirmed.");
        }

        OrderNumber = orderNumber;
        OrderDate = orderDate;
        Status = OrderStatus.Open;
        ConfirmedAtUtc = nowUtc;

        var ticket = CreateTicket(KitchenTicketKind.New, nowUtc);
        foreach (var item in ActiveItems)
        {
            ticket.AddLine(item.Id, item.MenuItemName, item.Quantity, item.SpecialInstructions, null);
        }

        return ticket;
    }

    /// <summary>
    /// Changes how many of a dish were ordered. Callers gate this behind an approval PIN once the
    /// order is open (BR-POS-007); the kitchen is told what the quantity used to be so the line
    /// cook can tell an amendment from a fresh order.
    /// </summary>
    public KitchenTicket? ChangeItemQuantity(Guid orderItemId, int quantity, DateTime nowUtc)
    {
        EnsureEditable();

        var item = FindItem(orderItemId);
        var previousQuantity = item.Quantity;

        if (previousQuantity == quantity)
        {
            return null;
        }

        item.ChangeQuantity(quantity);

        if (Status != OrderStatus.Open)
        {
            return null;
        }

        var ticket = CreateTicket(KitchenTicketKind.Modification, nowUtc);
        ticket.AddLine(item.Id, item.MenuItemName, item.Quantity, item.SpecialInstructions, $"Was {previousQuantity}");

        return ticket;
    }

    /// <summary>
    /// Takes a dish off the bill. While the order is a draft the line simply disappears — the
    /// kitchen never heard about it. Once open the line is kept and voided instead, so the bill
    /// still shows what was taken off and why the kitchen got a cancellation slip (POS-020).
    /// </summary>
    public KitchenTicket? RemoveItem(Guid orderItemId, DateTime nowUtc)
    {
        EnsureEditable();

        var item = FindItem(orderItemId);

        if (Status != OrderStatus.Open)
        {
            _items.Remove(item);
            return null;
        }

        item.Cancel(nowUtc);

        var ticket = CreateTicket(KitchenTicketKind.Cancellation, nowUtc);
        ticket.AddLine(item.Id, item.MenuItemName, item.Quantity, item.SpecialInstructions, "CANCELLED");

        return ticket;
    }

    /// <summary>Applies money off the bill (POS-007).</summary>
    public void SetDiscount(DiscountType type, decimal value)
    {
        EnsureEditable();

        switch (type)
        {
            case DiscountType.None:
                DiscountType = DiscountType.None;
                DiscountValue = 0m;
                return;

            case DiscountType.Percentage when value is < 0m or > 100m:
                throw new ArgumentOutOfRangeException(nameof(value), value, "A percentage discount must be between 0 and 100.");

            case DiscountType.Fixed when value < 0m:
                throw new ArgumentOutOfRangeException(nameof(value), value, "A discount cannot be negative.");

            case DiscountType.Fixed when value > Subtotal:
                throw new ArgumentOutOfRangeException(nameof(value), value, "A discount cannot exceed the order subtotal.");

            default:
                break;
        }

        DiscountType = type;
        DiscountValue = value;
    }

    /// <summary>Freezes the bill so it cannot move while the customer is paying.</summary>
    public void StartCheckout()
    {
        if (Status != OrderStatus.Open)
        {
            throw new InvalidOperationException("Only an open order can be checked out.");
        }

        Status = OrderStatus.Checkout;
    }

    /// <summary>Backs out of the payment screen, reopening the bill for more items.</summary>
    public void ReturnToOpen()
    {
        if (Status != OrderStatus.Checkout)
        {
            throw new InvalidOperationException("Only an order in checkout can be reopened.");
        }

        _payments.Clear();
        Status = OrderStatus.Open;
    }

    /// <summary>Records one tender against the bill (POS-023).</summary>
    public OrderPayment AddPayment(
        OrderPaymentMethod method, decimal amount, decimal? tenderedAmount, string? reference)
    {
        if (Status != OrderStatus.Checkout)
        {
            throw new InvalidOperationException("Payments can only be taken during checkout.");
        }

        var payment = new OrderPayment(Id, method, amount, tenderedAmount, reference);
        _payments.Add(payment);

        return payment;
    }

    /// <summary>
    /// Settles the bill and issues the receipt (POS-025), which releases the table (POS-032). The
    /// tenders must come to the bill exactly (BR-POS-013).
    /// </summary>
    public Receipt Complete(string receiptNumber, DateTime nowUtc)
    {
        if (Status != OrderStatus.Checkout)
        {
            throw new InvalidOperationException("Only an order in checkout can be completed.");
        }

        if (AmountPaid != Total)
        {
            throw new InvalidOperationException("The payments taken must equal the order total.");
        }

        Status = OrderStatus.Completed;
        CompletedAtUtc = nowUtc;
        Receipt = new Receipt(Id, receiptNumber, nowUtc);

        return Receipt;
    }

    /// <summary>
    /// Abandons the order without payment, releasing the table. Requires an approval PIN
    /// (BR-POS-009); the kitchen gets one cancellation slip covering everything still live.
    /// </summary>
    public KitchenTicket? Cancel(Guid cancelledByUserId, DateTime nowUtc)
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("A completed or already-cancelled order cannot be cancelled.");
        }

        var wasSentToKitchen = Status is OrderStatus.Open or OrderStatus.Checkout;
        var liveItems = ActiveItems.ToList();

        foreach (var item in liveItems)
        {
            item.Cancel(nowUtc);
        }

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = nowUtc;
        CancelledByUserId = cancelledByUserId;
        _payments.Clear();

        if (!wasSentToKitchen || liveItems.Count == 0)
        {
            return null;
        }

        var ticket = CreateTicket(KitchenTicketKind.Cancellation, nowUtc);
        foreach (var item in liveItems)
        {
            ticket.AddLine(item.Id, item.MenuItemName, item.Quantity, item.SpecialInstructions, "ORDER CANCELLED");
        }

        return ticket;
    }

    private KitchenTicket CreateTicket(KitchenTicketKind kind, DateTime nowUtc)
    {
        var ticket = new KitchenTicket(Id, _tickets.Count + 1, kind, nowUtc);
        _tickets.Add(ticket);

        return ticket;
    }

    private OrderItem FindItem(Guid orderItemId) =>
        _items.FirstOrDefault(i => i.Id == orderItemId)
        ?? throw new InvalidOperationException("That item is not on this order.");

    private void EnsureEditable()
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.Open))
        {
            throw new InvalidOperationException($"An order that is {Status} can no longer be edited.");
        }
    }
}
