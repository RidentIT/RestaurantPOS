import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "@/features/auth";
import { LoadingState } from "@/shared/ui";

/**
 * Gate for screens meant only for a signed-out visitor, namely `/login`.
 *
 * A signed-in user who lands here (back button, stale bookmark) is sent to the right place
 * instead of seeing the login form again.
 */
export function RequireGuest() {
  const { isAuthenticated, isResolving, mustChangePassword } = useAuth();

  if (isResolving) {
    return <LoadingState label="Checking your session…" className="h-screen" />;
  }

  if (isAuthenticated) {
    return <Navigate to={mustChangePassword ? "/change-password" : "/"} replace />;
  }

  return <Outlet />;
}
