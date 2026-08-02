import { Navigate, Outlet } from "react-router-dom";
import type { ModuleKey } from "@/entities/user";
import { useAuth } from "@/features/auth";

export interface RequireModuleProps {
  module: ModuleKey;
}

/**
 * Gate for a screen belonging to a specific module.
 *
 * This is a UX convenience, not the security boundary — every endpoint the page calls enforces
 * the same module grant server-side (`AuthorizationPolicies.ForModule`), so at worst a user
 * without access sees an empty screen's requests fail, never real data.
 */
export function RequireModule({ module }: RequireModuleProps) {
  const { can } = useAuth();

  return can(module) ? <Outlet /> : <Navigate to="/" replace />;
}
