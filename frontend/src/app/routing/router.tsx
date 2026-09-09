import { lazy, ReactNode, Suspense } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";
import { LoadingState } from "@/shared/ui";
import { AppShell } from "@/widgets/app-shell/AppShell";
import { RequireAuth } from "./RequireAuth";
import { RequireGuest } from "./RequireGuest";
import { RequireModule } from "./RequireModule";

// Lazy-loaded so the initial bundle only carries what the login screen needs; everything else
// loads once a session is established.
const LoginPage = lazy(() => import("@/pages/login"));
const ChangePasswordPage = lazy(() => import("@/pages/change-password"));
const DashboardPage = lazy(() => import("@/pages/dashboard"));
const AccountPage = lazy(() => import("@/pages/account"));
const UsersPage = lazy(() => import("@/pages/users"));
const ReportsPage = lazy(() => import("@/pages/reports"));
const RecipesPage = lazy(() => import("@/pages/recipes"));
const MenuItemDetailPage = lazy(() => import("@/pages/recipes/item"));
const MainStorePage = lazy(() => import("@/pages/inventory/main-store"));
const KitchenPage = lazy(() => import("@/pages/inventory/kitchen"));
const ReleasesPage = lazy(() => import("@/pages/inventory/releases"));
const SuppliersPage = lazy(() => import("@/pages/suppliers"));
const PosDashboardPage = lazy(() => import("@/pages/pos"));
const TableManagementPage = lazy(() => import("@/pages/pos/tables"));
const OrderScreen = lazy(() => import("@/pages/pos/order"));
const CheckoutScreen = lazy(() => import("@/pages/pos/checkout"));
const KitchenDisplayPage = lazy(() => import("@/pages/kitchen"));
const ExpensesPage = lazy(() => import("@/pages/expenses"));
const ExpenseCategoriesPage = lazy(() => import("@/pages/expenses/categories"));
const RecurringExpensesPage = lazy(() => import("@/pages/expenses/recurring"));
const ExpenseReportsPage = lazy(() => import("@/pages/expenses/reports"));
const NotificationSettingsPage = lazy(() => import("@/pages/notifications/settings"));
const SettingsPage = lazy(() => import("@/pages/settings"));

function withSuspense(element: ReactNode) {
  return <Suspense fallback={<LoadingState label="Loading…" className="h-screen" />}>{element}</Suspense>;
}

export const router = createBrowserRouter([
  {
    element: <RequireGuest />,
    children: [{ path: "/login", element: withSuspense(<LoginPage />) }],
  },
  {
    element: <RequireAuth />,
    children: [
      // Reachable the instant a session exists, even mid forced-password-change.
      { path: "/change-password", element: withSuspense(<ChangePasswordPage />) },
      {
        element: <AppShell />,
        children: [
          { index: true, element: withSuspense(<DashboardPage />) },
          { path: "account", element: withSuspense(<AccountPage />) },
          // Everybody has a bell, so everybody can configure it — what each person is
          // eligible to receive is decided server-side by the modules they hold.
          { path: "notifications/settings", element: withSuspense(<NotificationSettingsPage />) },
          {
            element: <RequireModule module="PosBilling" />,
            children: [
              { path: "pos", element: withSuspense(<PosDashboardPage />) },
              { path: "pos/tables", element: withSuspense(<TableManagementPage />) },
              { path: "pos/orders/:orderId", element: withSuspense(<OrderScreen />) },
              { path: "pos/orders/:orderId/checkout", element: withSuspense(<CheckoutScreen />) },
              { path: "checkout", element: <Navigate to="/pos" replace /> },
            ],
          },
          {
            element: <RequireModule module="KitchenOperations" />,
            children: [{ path: "kitchen", element: withSuspense(<KitchenDisplayPage />) }],
          },
          {
            element: <RequireModule module="ExpensesManagement" />,
            children: [
              { path: "expenses", element: withSuspense(<ExpensesPage />) },
              { path: "expenses/categories", element: withSuspense(<ExpenseCategoriesPage />) },
              { path: "expenses/recurring", element: withSuspense(<RecurringExpensesPage />) },
              { path: "expenses/reports", element: withSuspense(<ExpenseReportsPage />) },
            ],
          },
          {
            element: <RequireModule module="ReportsAnalytics" />,
            children: [{ path: "reports", element: withSuspense(<ReportsPage />) }],
          },
          {
            element: <RequireModule module="UserManagement" />,
            children: [{ path: "users", element: withSuspense(<UsersPage />) }],
          },
          {
            element: <RequireModule module="RecipeManagement" />,
            children: [
              { path: "recipes", element: withSuspense(<RecipesPage />) },
              // Listed before the ":id" route: react-router ranks a static segment ahead of a
              // dynamic one regardless of order, but this keeps the intent obvious on read.
              { path: "recipes/new", element: withSuspense(<MenuItemDetailPage />) },
              { path: "recipes/:id", element: withSuspense(<MenuItemDetailPage />) },
            ],
          },
          {
            element: <RequireModule module="StoreStockManagement" />,
            children: [{ path: "inventory/main-store", element: withSuspense(<MainStorePage />) }],
          },
          {
            element: <RequireModule module="KitchenStockTracking" />,
            children: [{ path: "inventory/kitchen", element: withSuspense(<KitchenPage />) }],
          },
          {
            element: <RequireModule module="KitchenStockRelease" />,
            children: [{ path: "inventory/releases", element: withSuspense(<ReleasesPage />) }],
          },
          {
            element: <RequireModule module="SupplierManagement" />,
            children: [{ path: "suppliers", element: withSuspense(<SuppliersPage />) }],
          },
          {
            element: <RequireModule module="SystemSettings" />,
            children: [{ path: "settings", element: withSuspense(<SettingsPage />) }],
          },
        ],
      },
    ],
  },
  { path: "*", element: <Navigate to="/" replace /> },
]);
