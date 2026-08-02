import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "@/features/auth";
import { LoadingState } from "@/shared/ui";

/**
 * Gate for every screen that requires a signed-in session.
 *
 * Also enforces the forced-password-change flow client-side: this mirrors the server's
 * `PasswordChangeRequiredMiddleware`, which is the real enforcement point, but redirecting here
 * too means a user with a pending change never even sees a screen the API would reject.
 */
export function RequireAuth() {
  const { isAuthenticated, isResolving, mustChangePassword } = useAuth();
  const location = useLocation();

  if (isResolving) {
    return <LoadingState label="Checking your session…" className="h-screen" />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (mustChangePassword && location.pathname !== "/change-password") {
    return <Navigate to="/change-password" replace />;
  }

  return <Outlet />;
}
