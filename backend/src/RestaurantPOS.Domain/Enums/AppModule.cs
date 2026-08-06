namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// Assignable functional areas of the POS. Values are persisted, so they must remain stable:
/// append new members with new numbers, never renumber existing ones.
/// </summary>
public enum AppModule
{
    PosBilling = 1,
    RecipeManagement = 2,
    StoreStockManagement = 3,
    KitchenStockRelease = 4,
    KitchenStockTracking = 5,
    KitchenOperations = 6,
    ReportsAnalytics = 7,
    UserManagement = 8,
    Notifications = 9,
    SupplierManagement = 10,
    ExpensesManagement = 11,
    SystemSettings = 12,
}