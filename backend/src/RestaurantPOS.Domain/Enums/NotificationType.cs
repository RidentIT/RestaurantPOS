namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// Every kind of alert the system can raise. The value is stored, so numbers are fixed once
/// assigned; new types are appended rather than inserted.
/// </summary>
public enum NotificationType
{
    // ----- POS & Billing -----

    /// <summary>The kitchen has plated everything for a table.</summary>
    FoodReadyToServe = 1,

    /// <summary>An order was written off after it had been confirmed.</summary>
    OrderCancelled = 2,

    /// <summary>A discount above the configured size was applied to a bill.</summary>
    LargeDiscountApplied = 3,

    /// <summary>A table has been sitting confirmed but unpaid for longer than expected.</summary>
    TableOpenTooLong = 4,

    // ----- Kitchen Operations -----

    /// <summary>A ticket has been on the pass longer than the configured wait.</summary>
    KitchenTicketWaitingTooLong = 10,

    /// <summary>A new ticket has reached the pass.</summary>
    NewKitchenTicket = 11,

    // ----- Stock -----

    /// <summary>A raw material has fallen to its Main Store reorder level.</summary>
    MainStoreLowStock = 20,

    /// <summary>A raw material has fallen to its Kitchen par level.</summary>
    KitchenLowStock = 21,

    /// <summary>
    /// A Kitchen balance has gone below zero, which means dishes were sold against stock nobody
    /// booked in. Sales are never blocked for want of stock, so this is the only warning anyone
    /// gets that the ledger and the shelf disagree.
    /// </summary>
    KitchenStockNegative = 22,

    /// <summary>Someone corrected a stock balance by hand.</summary>
    StockAdjustmentRecorded = 23,

    // ----- Suppliers -----

    /// <summary>A confirmed purchase order is past the date it was due to arrive.</summary>
    PurchaseOrderDeliveryOverdue = 30,

    /// <summary>A delivered purchase order still has money owing on it.</summary>
    SupplierPaymentDue = 31,

    /// <summary>A submitted purchase order has not been confirmed by the supplier.</summary>
    PurchaseOrderNotConfirmed = 32,

    /// <summary>A delivery arrived with a problem, or was rated poorly.</summary>
    GoodsReceivedIssue = 33,

    /// <summary>A supplier's price rose by more than the configured percentage.</summary>
    SupplierPriceIncreased = 34,

    // ----- Expenses -----

    /// <summary>Expenses are sitting waiting for a manager to rule on them.</summary>
    ExpensesAwaitingApproval = 40,

    /// <summary>A category has spent past its monthly budget.</summary>
    ExpenseCategoryOverBudget = 41,

    /// <summary>A category is close to its monthly budget.</summary>
    ExpenseCategoryNearingBudget = 42,

    /// <summary>An expense you recorded was turned down.</summary>
    ExpenseRejected = 43,

    /// <summary>This month's standing costs have been created and need reviewing.</summary>
    RecurringExpensesCreated = 44,

    /// <summary>An approved expense is past its payment date and still unpaid.</summary>
    ExpenseOverdueUnpaid = 45,

    // ----- Administration -----

    /// <summary>A staff account was created, changed or deactivated.</summary>
    UserAccountChanged = 50,

    /// <summary>A menu item is on sale with no recipe, so selling it deducts nothing.</summary>
    MenuItemWithoutRecipe = 60,
}
