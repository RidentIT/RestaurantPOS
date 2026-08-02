using System.Collections.ObjectModel;

using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Modules;

/// <summary>
/// The authoritative list of assignable modules. The frontend renders its navigation and
/// permission editor from this catalog, so adding a module here is all that is needed to
/// make it grantable.
/// </summary>
public static class ModuleCatalog
{
    private const string InventoryGroup = "Inventory Management";
    private const string OperationsGroup = "Operations";
    private const string AdministrationGroup = "Administration";

    private static readonly ModuleDescriptor[] Descriptors =
    [
        new(AppModule.PosBilling, "POS & Billing", OperationsGroup,
            "Take orders, split and settle bills, and print receipts.", 10),

        new(AppModule.RecipeManagement, "Recipe Management", OperationsGroup,
            "Define dishes and the ingredient quantities each one consumes.", 20),

        new(AppModule.StoreStockManagement, "Store Stock Management", InventoryGroup,
            "Receive goods into the main store and maintain stock levels.", 30),

        new(AppModule.KitchenStockRelease, "Kitchen Stock Release", InventoryGroup,
            "Issue stock from the main store to the kitchen.", 40),

        new(AppModule.KitchenStockTracking, "Kitchen Stock Tracking", InventoryGroup,
            "Track consumption and wastage of stock held by the kitchen.", 50),

        new(AppModule.KitchenOperations, "Kitchen Operations", OperationsGroup,
            "Kitchen display, ticket queue and preparation status.", 60),

        new(AppModule.SupplierManagement, "Supplier Management", OperationsGroup,
            "Maintain suppliers, purchase orders and goods received notes.", 70),

        new(AppModule.ExpensesManagement, "Expenses Management", OperationsGroup,
            "Record and categorise day-to-day operating expenses.", 80),

        new(AppModule.ReportsAnalytics, "Reports & Analytics", AdministrationGroup,
            "Sales, inventory, expense and staff performance reporting.", 90),

        new(AppModule.Notifications, "Notifications", AdministrationGroup,
            "Low-stock, approval and operational alerts.", 100),

        new(AppModule.UserManagement, "User Management & Roles", AdministrationGroup,
            "Create staff accounts and control which modules they can open.", 110, AdminOnly: true),

        new(AppModule.SystemSettings, "System Settings & Backup", AdministrationGroup,
            "Restaurant details, tax and printer settings, and database backups.", 120, AdminOnly: true),
    ];

    /// <summary>All assignable modules in display order.</summary>
    public static IReadOnlyList<ModuleDescriptor> All { get; } =
        new ReadOnlyCollection<ModuleDescriptor>(
            [.. Descriptors.OrderBy(d => d.SortOrder)]);

    /// <summary>Modules an administrator may grant to a non-admin user.</summary>
    public static IReadOnlyList<ModuleDescriptor> Assignable { get; } =
        new ReadOnlyCollection<ModuleDescriptor>(
            [.. Descriptors.Where(d => !d.AdminOnly).OrderBy(d => d.SortOrder)]);

    private static readonly Dictionary<AppModule, ModuleDescriptor> ByModule =
        Descriptors.ToDictionary(d => d.Module);

    /// <summary>Returns true when the value maps to a module in the catalog.</summary>
    public static bool IsDefined(AppModule module) => ByModule.ContainsKey(module);

    /// <summary>True when the module may be granted to a non-admin user.</summary>
    public static bool IsAssignableToUser(AppModule module) =>
        ByModule.TryGetValue(module, out var descriptor) && !descriptor.AdminOnly;

    /// <summary>Looks up display metadata for a module.</summary>
    /// <exception cref="KeyNotFoundException">The module is not in the catalog.</exception>
    public static ModuleDescriptor Describe(AppModule module) => ByModule[module];
}