using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>
/// The persistence surface the application layer is allowed to touch. Keeping handlers on
/// this abstraction rather than the concrete <c>AppDbContext</c> preserves the dependency
/// rule enforced by the architecture tests.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<UserModulePermission> UserModulePermissions { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<MenuItem> MenuItems { get; }

    DbSet<RawMaterial> RawMaterials { get; }

    DbSet<Recipe> Recipes { get; }

    DbSet<Supplier> Suppliers { get; }

    DbSet<StockLevel> StockLevels { get; }

    DbSet<StockMovement> StockMovements { get; }

    DbSet<GoodsReceivedNote> GoodsReceivedNotes { get; }

    DbSet<StockRelease> StockReleases { get; }

    DbSet<AuditLogEntry> AuditLogEntries { get; }

    DbSet<PurchaseOrder> PurchaseOrders { get; }

    DbSet<SupplierPrice> SupplierPrices { get; }

    DbSet<SupplierPriceHistoryEntry> SupplierPriceHistoryEntries { get; }

    DbSet<SupplierPayment> SupplierPayments { get; }

    DbSet<RestaurantTable> RestaurantTables { get; }

    DbSet<Order> Orders { get; }

    DbSet<OrderItem> OrderItems { get; }

    DbSet<KitchenTicket> KitchenTickets { get; }

    DbSet<OrderPayment> OrderPayments { get; }

    DbSet<Receipt> Receipts { get; }

    DbSet<ExpenseCategory> ExpenseCategories { get; }

    DbSet<Expense> Expenses { get; }

    DbSet<ExpenseAttachment> ExpenseAttachments { get; }

    DbSet<ExpenseApprovalEntry> ExpenseApprovalEntries { get; }

    DbSet<RecurringExpense> RecurringExpenses { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<NotificationPreference> NotificationPreferences { get; }

    DbSet<NotificationSetting> NotificationSettings { get; }

    DbSet<RestaurantSettings> RestaurantSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}