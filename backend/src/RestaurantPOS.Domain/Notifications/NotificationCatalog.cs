using System.Collections.ObjectModel;

using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Notifications;

/// <summary>
/// The authoritative list of notifications the system can raise.
/// </summary>
/// <remarks>
/// Mirrors <see cref="Modules.ModuleCatalog"/>: one place that says what exists, so the settings
/// screen, the routing rules and the defaults can never drift apart. Adding a notification means
/// adding a descriptor here and a rule that produces it — nothing else.
/// </remarks>
public static class NotificationCatalog
{
    private static readonly NotificationDescriptor[] Descriptors =
    [
        // ----- POS & Billing -----
        new(NotificationType.FoodReadyToServe,
            "Food ready to serve",
            "The kitchen has plated everything for a table.",
            [AppModule.PosBilling],
            NotificationSeverity.Urgent,
            DefaultEnabled: true),

        new(NotificationType.OrderCancelled,
            "Order cancelled",
            "A confirmed order was written off. The kitchen is told to stop cooking.",
            [AppModule.PosBilling, AppModule.KitchenOperations],
            NotificationSeverity.Urgent,
            DefaultEnabled: true),

        new(NotificationType.LargeDiscountApplied,
            "Large discount applied",
            "A discount above the set amount was taken off a bill.",
            [AppModule.PosBilling],
            NotificationSeverity.Warning,
            DefaultEnabled: false,
            ThresholdUnit.Amount,
            DefaultThreshold: 1000m),

        new(NotificationType.TableOpenTooLong,
            "Table open too long",
            "A table has been confirmed but unpaid for longer than expected.",
            [AppModule.PosBilling],
            NotificationSeverity.Info,
            DefaultEnabled: false,
            ThresholdUnit.Minutes,
            DefaultThreshold: 120m),

        // ----- Kitchen Operations -----
        new(NotificationType.KitchenTicketWaitingTooLong,
            "Ticket waiting too long",
            "A ticket has been on the pass longer than the set wait.",
            [AppModule.KitchenOperations],
            NotificationSeverity.Warning,
            DefaultEnabled: true,
            ThresholdUnit.Minutes,
            DefaultThreshold: 15m),

        new(NotificationType.NewKitchenTicket,
            "New ticket arrived",
            "A new order reached the pass. The kitchen display already refreshes itself.",
            [AppModule.KitchenOperations],
            NotificationSeverity.Info,
            DefaultEnabled: false),

        // ----- Stock -----
        new(NotificationType.MainStoreLowStock,
            "Main Store running low",
            "A raw material has reached its reorder level.",
            [AppModule.StoreStockManagement],
            NotificationSeverity.Warning,
            DefaultEnabled: true),

        new(NotificationType.KitchenLowStock,
            "Kitchen running low",
            "A raw material has reached its kitchen par level.",
            [AppModule.KitchenStockTracking, AppModule.KitchenStockRelease],
            NotificationSeverity.Warning,
            DefaultEnabled: true),

        new(NotificationType.KitchenStockNegative,
            "Kitchen stock went negative",
            "Dishes were sold against stock that was never booked in.",
            [AppModule.KitchenStockTracking, AppModule.StoreStockManagement],
            NotificationSeverity.Urgent,
            DefaultEnabled: true),

        new(NotificationType.StockAdjustmentRecorded,
            "Stock adjusted by hand",
            "Someone corrected a balance outside the normal flow.",
            [AppModule.StoreStockManagement, AppModule.KitchenStockTracking],
            NotificationSeverity.Info,
            DefaultEnabled: false),

        // ----- Suppliers -----
        new(NotificationType.PurchaseOrderDeliveryOverdue,
            "Delivery overdue",
            "A confirmed purchase order is past the date it was due.",
            [AppModule.SupplierManagement],
            NotificationSeverity.Warning,
            DefaultEnabled: true),

        new(NotificationType.SupplierPaymentDue,
            "Payment due to supplier",
            "A delivered order still has money owing, past the supplier's terms.",
            [AppModule.SupplierManagement],
            NotificationSeverity.Warning,
            DefaultEnabled: true),

        new(NotificationType.PurchaseOrderNotConfirmed,
            "Purchase order not confirmed",
            "A submitted order has had no response from the supplier.",
            [AppModule.SupplierManagement],
            NotificationSeverity.Info,
            DefaultEnabled: false,
            ThresholdUnit.Days,
            DefaultThreshold: 3m),

        new(NotificationType.GoodsReceivedIssue,
            "Delivery had a problem",
            "A delivery was flagged with an issue or rated poorly.",
            [AppModule.SupplierManagement, AppModule.StoreStockManagement],
            NotificationSeverity.Warning,
            DefaultEnabled: true),

        new(NotificationType.SupplierPriceIncreased,
            "Supplier raised a price",
            "A supplier's price rose by more than the set percentage.",
            [AppModule.SupplierManagement],
            NotificationSeverity.Info,
            DefaultEnabled: false,
            ThresholdUnit.Percent,
            DefaultThreshold: 10m),

        // ----- Expenses -----
        new(NotificationType.ExpensesAwaitingApproval,
            "Expenses awaiting approval",
            "Expenses are sitting waiting for a decision.",
            [AppModule.ExpensesManagement],
            NotificationSeverity.Warning,
            DefaultEnabled: true),

        new(NotificationType.ExpenseCategoryOverBudget,
            "Category over budget",
            "A category has spent past its monthly budget.",
            [AppModule.ExpensesManagement],
            NotificationSeverity.Warning,
            DefaultEnabled: true),

        new(NotificationType.ExpenseCategoryNearingBudget,
            "Category nearing budget",
            "A category is close to its monthly budget.",
            [AppModule.ExpensesManagement],
            NotificationSeverity.Info,
            DefaultEnabled: false,
            ThresholdUnit.Percent,
            DefaultThreshold: 80m),

        new(NotificationType.ExpenseRejected,
            "Your expense was rejected",
            "An expense you recorded was turned down.",
            [AppModule.ExpensesManagement],
            NotificationSeverity.Warning,
            DefaultEnabled: true),

        new(NotificationType.RecurringExpensesCreated,
            "Recurring expenses created",
            "This month's standing costs appeared as drafts and need reviewing.",
            [AppModule.ExpensesManagement],
            NotificationSeverity.Info,
            DefaultEnabled: true),

        new(NotificationType.ExpenseOverdueUnpaid,
            "Expense still unpaid",
            "An approved expense is past its payment date and has not been paid.",
            [AppModule.ExpensesManagement],
            NotificationSeverity.Warning,
            DefaultEnabled: false),

        // ----- Administration -----
        new(NotificationType.UserAccountChanged,
            "Staff account changed",
            "An account was created, changed or deactivated.",
            [AppModule.UserManagement],
            NotificationSeverity.Info,
            DefaultEnabled: false),

        new(NotificationType.MenuItemWithoutRecipe,
            "Menu item has no recipe",
            "The item sells without ever deducting stock.",
            [AppModule.RecipeManagement],
            NotificationSeverity.Info,
            DefaultEnabled: false),
    ];

    /// <summary>Every notification the system can raise.</summary>
    public static IReadOnlyList<NotificationDescriptor> All { get; } =
        new ReadOnlyCollection<NotificationDescriptor>(Descriptors);

    private static readonly Dictionary<NotificationType, NotificationDescriptor> ByType =
        Descriptors.ToDictionary(d => d.Type);

    /// <summary>Looks up what is known about a notification type.</summary>
    /// <exception cref="KeyNotFoundException">The type is not in the catalog.</exception>
    public static NotificationDescriptor Describe(NotificationType type) => ByType[type];

    /// <summary>Returns true when the value maps to a notification in the catalog.</summary>
    public static bool IsDefined(NotificationType type) => ByType.ContainsKey(type);

    /// <summary>
    /// The notifications somebody holding <paramref name="modules"/> is eligible to receive.
    /// Administrators hold every module implicitly and so are eligible for all of them.
    /// </summary>
    public static IEnumerable<NotificationDescriptor> ForModules(
        IReadOnlyCollection<AppModule> modules, bool isAdmin)
    {
        ArgumentNullException.ThrowIfNull(modules);

        return isAdmin ? Descriptors : Descriptors.Where(d => d.Modules.Any(modules.Contains));
    }
}
