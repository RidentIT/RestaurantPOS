import {
  LayoutDashboard,
  ChefHat,
  ClipboardList,
  PackageSearch,
  Truck,
  Warehouse,
  Receipt,
  BarChart3,
  Bell,
  Users,
  Settings,
  type LucideIcon,
} from "lucide-react";

/**
 * Where each module lives in the app, and what to show for it in the sidebar before its screen
 * exists yet.
 *
 * This is the one place a module goes from "in the catalog" to "has a page" — add a `path` here
 * once its screen is built. Until then the module still appears in navigation (so the menu
 * structure matches what the restaurant was promised) as a disabled "coming soon" item.
 *
 * Lives in `shared` rather than `entities/user` (where `ModuleKey` is defined) or `app` (where
 * the router lives) because it is consumed by both a widget (the sidebar) and pages — the FSD
 * boundary rules this project enforces mean `shared` is the only layer both can reach. The map
 * is keyed by the same plain-string module identifier as `ModuleKey`, just without importing it,
 * so this file carries no dependency on the entity layer.
 */
export interface ModuleRoute {
  /** Present once the module's screen exists; absent renders as a disabled entry. */
  path?: string;
  icon: LucideIcon;
}

export const MODULE_ROUTES: Record<string, ModuleRoute> = {
  PosBilling: { path: "/pos", icon: Receipt },
  RecipeManagement: { path: "/recipes", icon: ChefHat },
  StoreStockManagement: { path: "/inventory/main-store", icon: Warehouse },
  KitchenStockRelease: { path: "/inventory/releases", icon: PackageSearch },
  KitchenStockTracking: { path: "/inventory/kitchen", icon: ClipboardList },
  KitchenOperations: { path: "/kitchen", icon: ChefHat },
  ReportsAnalytics: { path: "/reports", icon: BarChart3 },
  Notifications: { icon: Bell },
  UserManagement: { path: "/users", icon: Users },
  SupplierManagement: { path: "/suppliers", icon: Truck },
  ExpensesManagement: { icon: Receipt },
  SystemSettings: { icon: Settings },
};

export const DEFAULT_MODULE_ICON: LucideIcon = LayoutDashboard;
