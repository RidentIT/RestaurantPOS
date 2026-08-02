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
const CheckoutPage = lazy(() => import("@/pages/checkout"));
const ReportsPage = lazy(() => import("@/pages/reports"));

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
          {
            element: <RequireModule module="PosBilling" />,
            children: [{ path: "checkout", element: withSuspense(<CheckoutPage />) }],
          },
          {
            element: <RequireModule module="ReportsAnalytics" />,
            children: [{ path: "reports", element: withSuspense(<ReportsPage />) }],
          },
          {
            element: <RequireModule module="UserManagement" />,
            children: [{ path: "users", element: withSuspense(<UsersPage />) }],
          },
        ],
      },
    ],
  },
  { path: "*", element: <Navigate to="/" replace /> },
]);
